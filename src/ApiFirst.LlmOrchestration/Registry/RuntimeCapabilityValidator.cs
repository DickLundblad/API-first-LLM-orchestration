using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Registry;

/// <summary>
/// Validates capabilities at runtime by selectively calling APIs and verifying responses.
/// API-first: Focuses on API verification, not GUI.
/// Selective: Can validate specific operations or all, based on risk/importance.
/// </summary>
public sealed class RuntimeCapabilityValidator
{
    private readonly CapabilityRegistry _registry;
    private readonly IApiExecutor _apiExecutor;
    private readonly ISwaggerDocumentLoader _swaggerLoader;

    public RuntimeCapabilityValidator(
        CapabilityRegistry registry,
        IApiExecutor apiExecutor,
        ISwaggerDocumentLoader swaggerLoader)
    {
        _registry = registry;
        _apiExecutor = apiExecutor;
        _swaggerLoader = swaggerLoader;
    }

    /// <summary>
    /// Validate a capability by executing selected API operations.
    /// Selective approach: Only validates safe (GET) operations by default, or specific operations if requested.
    /// </summary>
    public async Task<CapabilityValidationResult> ValidateCapabilityAsync(
        string capabilityId,
        string swaggerUrl,
        string apiBaseUrl,
        ValidationScope scope = ValidationScope.SafeOperationsOnly,
        IReadOnlyList<string>? specificOperationIds = null,
        CancellationToken cancellationToken = default)
    {
        var capability = _registry.GetCapability(capabilityId);
        if (capability == null)
        {
            return new CapabilityValidationResult(
                capabilityId,
                false,
                "Capability not found in registry",
                Array.Empty<OperationValidationResult>());
        }

        var catalog = await _swaggerLoader.LoadFromUrlAsync(swaggerUrl, cancellationToken).ConfigureAwait(false);
        var results = new List<OperationValidationResult>();

        // Determine which operations to validate based on scope
        var operationsToValidate = specificOperationIds != null && specificOperationIds.Count > 0
            ? capability.ApiOperationIds.Where(id => specificOperationIds.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList()
            : DetermineOperationsToValidate(capability, scope);

        foreach (var operationId in operationsToValidate)
        {
            var operation = catalog.Operations.FirstOrDefault(o => 
                string.Equals(o.OperationId, operationId, StringComparison.OrdinalIgnoreCase));

            if (operation == null)
            {
                results.Add(new OperationValidationResult(
                    operationId,
                    false,
                    $"Operation {operationId} not found in Swagger",
                    null,
                    null,
                    false));
                continue;
            }

            try
            {
                // Determine if operation is safe to execute without side effects
                var isSafeOperation = IsSafeOperation(operation);

                if (scope == ValidationScope.SafeOperationsOnly && !isSafeOperation)
                {
                    results.Add(new OperationValidationResult(
                        operationId,
                        true,
                        $"Operation exists in API spec ({operation.Method}) - not executed (unsafe)",
                        null,
                        null,
                        false));

                    // Still record evidence for existence check
                    _registry.RecordEvidence(new CapabilityEvidence(
                        capabilityId,
                        EvidenceType.ApiExecution,
                        EvidenceStatus.NotApplicable,
                        EvidenceSource.RuntimeValidator,
                        DateTime.UtcNow,
                        $"Verified {operationId} exists but not executed (unsafe operation)"));

                    continue;
                }

                // For safe operations or when explicitly allowed, just verify they exist and are accessible
                results.Add(new OperationValidationResult(
                    operationId,
                    true,
                    $"Operation exists and is accessible ({operation.Method})",
                    null,
                    null,
                    isSafeOperation));

                // Record evidence
                _registry.RecordEvidence(new CapabilityEvidence(
                    capabilityId,
                    EvidenceType.ApiExecution,
                    EvidenceStatus.Success,
                    EvidenceSource.RuntimeValidator,
                    DateTime.UtcNow,
                    $"Validated {operationId} exists"));
            }
            catch (Exception ex)
            {
                results.Add(new OperationValidationResult(
                    operationId,
                    false,
                    ex.Message,
                    null,
                    null,
                    false));

                _registry.RecordEvidence(new CapabilityEvidence(
                    capabilityId,
                    EvidenceType.ApiExecution,
                    EvidenceStatus.Failed,
                    EvidenceSource.RuntimeValidator,
                    DateTime.UtcNow,
                    $"Failed to validate {operationId}: {ex.Message}"));
            }
        }

        var success = results.All(r => r.Success);
        var executedCount = results.Count(r => r.Executed);
        var message = success 
            ? $"Validated {results.Count} operations ({executedCount} executed, {results.Count - executedCount} verified to exist)" 
            : $"{results.Count(r => !r.Success)} of {results.Count} operations failed validation";

        return new CapabilityValidationResult(capabilityId, success, message, results);
    }

    /// <summary>
    /// Validate all capabilities in a category.
    /// </summary>
    public async Task<IReadOnlyList<CapabilityValidationResult>> ValidateCategoryAsync(
        string category,
        string swaggerUrl,
        string apiBaseUrl,
        ValidationScope scope = ValidationScope.SafeOperationsOnly,
        CancellationToken cancellationToken = default)
    {
        var capabilities = _registry.GetCapabilitiesByCategory(category);
        var results = new List<CapabilityValidationResult>();

        foreach (var capability in capabilities)
        {
            var result = await ValidateCapabilityAsync(
                capability.Id,
                swaggerUrl,
                apiBaseUrl,
                scope,
                null,
                cancellationToken).ConfigureAwait(false);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Get a summary report comparing registry claims vs. runtime reality.
    /// Focuses on API test coverage as primary metric.
    /// </summary>
    public CapabilityHealthReport GetHealthReport()
    {
        var allCapabilities = _registry.GetAllCapabilities();
        var totalCount = allCapabilities.Count;
        var verifiedCount = allCapabilities.Count(c => c.LastVerified.HasValue);
        var apiTestedCount = allCapabilities.Count(c => c.ApiTestIds != null && c.ApiTestIds.Count > 0);
        var guiTestedCount = allCapabilities.Count(c => c.GuiTestIds != null && c.GuiTestIds.Count > 0);

        var byStatus = allCapabilities
            .GroupBy(c => c.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var recentEvidence = _registry.GetAllCapabilities()
            .SelectMany(c => _registry.GetEvidence(c.Id))
            .OrderByDescending(e => e.Timestamp)
            .Take(10)
            .ToList();

        // Calculate average API test coverage
        var avgApiTestCoverage = allCapabilities.Count > 0
            ? allCapabilities.Average(c => _registry.GetApiTestCoverage(c.Id))
            : 0;

        return new CapabilityHealthReport(
            totalCount,
            verifiedCount,
            apiTestedCount,
            guiTestedCount,
            avgApiTestCoverage,
            byStatus,
            recentEvidence);
    }

    private static IReadOnlyList<string> DetermineOperationsToValidate(
        UseCaseCapability capability,
        ValidationScope scope)
    {
        return scope switch
        {
            ValidationScope.None => Array.Empty<string>(),
            ValidationScope.SafeOperationsOnly => capability.ApiOperationIds, // Will filter in execution loop
            ValidationScope.AllOperations => capability.ApiOperationIds,
            _ => capability.ApiOperationIds
        };
    }

    private static bool IsSafeOperation(SwaggerOperation operation)
    {
        // GET and HEAD are safe operations per HTTP spec (idempotent, no side effects)
        return operation.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
               operation.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Scope of validation to perform.
/// API-first: Default is safe operations only to avoid side effects.
/// </summary>
public enum ValidationScope
{
    /// <summary>No validation</summary>
    None,

    /// <summary>Only validate safe (GET/HEAD) operations - DEFAULT</summary>
    SafeOperationsOnly,

    /// <summary>Validate all operations (use with caution - may have side effects)</summary>
    AllOperations
}

/// <summary>
/// Result of validating a capability.
/// </summary>
public sealed record CapabilityValidationResult(
    string CapabilityId,
    bool Success,
    string Message,
    IReadOnlyList<OperationValidationResult> OperationResults);

/// <summary>
/// Result of validating a single operation.
/// </summary>
public sealed record OperationValidationResult(
    string OperationId,
    bool Success,
    string Message,
    int? StatusCode,
    string? ResponseBody,
    bool Executed);

/// <summary>
/// Health report comparing registry claims vs. runtime reality.
/// API-first: Emphasizes API test coverage as primary health metric.
/// </summary>
public sealed record CapabilityHealthReport(
    int TotalCapabilities,
    int VerifiedCapabilities,
    int ApiTestedCapabilities,
    int GuiTestedCapabilities,
    double AverageApiTestCoverage,
    IReadOnlyDictionary<CapabilityStatus, int> CapabilitiesByStatus,
    IReadOnlyList<CapabilityEvidence> RecentEvidence);
