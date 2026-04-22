using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Registry;

/// <summary>
/// Generates capabilities dynamically from Swagger endpoints.
/// API-first: Each endpoint becomes a potential capability.
/// No pre-configured use cases - derive from what the API declares.
/// </summary>
public sealed class CapabilityGenerator
{
    /// <summary>
    /// Generate capabilities from a Swagger catalog.
    /// Each operation becomes a basic capability.
    /// Groups related operations heuristically by resource.
    /// </summary>
    public static IReadOnlyList<UseCaseCapability> GenerateFromSwagger(SwaggerDocumentCatalog catalog)
    {
        var capabilities = new List<UseCaseCapability>();

        // Group operations by resource (first tag or path segment)
        var operationGroups = catalog.Operations
            .GroupBy(op => GetResourceName(op))
            .ToList();

        foreach (var group in operationGroups)
        {
            var resourceName = group.Key;
            var operations = group.ToList();

            // Create a capability for each operation (fine-grained)
            foreach (var operation in operations)
            {
                var capability = CreateCapabilityFromOperation(operation);
                capabilities.Add(capability);
            }

            // Also create a grouped capability if multiple operations exist
            if (operations.Count > 1)
            {
                var groupedCapability = CreateGroupedCapability(resourceName, operations);
                capabilities.Add(groupedCapability);
            }
        }

        return capabilities;
    }

    /// <summary>
    /// Create a capability from a single operation.
    /// </summary>
    private static UseCaseCapability CreateCapabilityFromOperation(SwaggerOperation operation)
    {
        var id = operation.OperationId.ToLowerInvariant();
        var category = operation.Tags.FirstOrDefault() ?? "General";
        var status = InferStatus(operation);

        return new UseCaseCapability(
            Id: id,
            Name: operation.Summary ?? operation.OperationId,
            Description: $"{operation.Method.ToUpperInvariant()} {operation.Path}",
            Category: category,
            Status: status,
            ApiOperationIds: new List<string> { operation.OperationId },
            ApiTestIds: new List<string>(),
            GuiRoute: null,
            GuiFeature: null,
            GuiTestIds: new List<string>(),
            BacklogItemIds: new List<string>(),
            Metadata: new Dictionary<string, string>
            {
                ["generatedFrom"] = "Swagger",
                ["httpMethod"] = operation.Method,
                ["path"] = operation.Path,
                ["hasRequestBody"] = operation.HasRequestBody.ToString(),
                ["requiresAuth"] = (operation.SecurityRequirements.Count > 0).ToString(),
                ["tags"] = string.Join(", ", operation.Tags)
            },
            LastVerified: null,
            RequiredEvidenceLevel: EvidenceLevel.ApiTests
        );
    }

    /// <summary>
    /// Create a grouped capability from multiple related operations.
    /// Example: All team member operations -> "team-management" capability
    /// </summary>
    private static UseCaseCapability CreateGroupedCapability(string resourceName, List<SwaggerOperation> operations)
    {
        var id = $"{resourceName.ToLowerInvariant()}-management";
        var category = operations.First().Tags.FirstOrDefault() ?? "General";

        return new UseCaseCapability(
            Id: id,
            Name: $"{resourceName} Management",
            Description: $"Combined operations for {resourceName}",
            Category: category,
            Status: CapabilityStatus.Planned,
            ApiOperationIds: operations.Select(op => op.OperationId).ToList(),
            ApiTestIds: new List<string>(),
            GuiRoute: null,
            GuiFeature: null,
            GuiTestIds: new List<string>(),
            BacklogItemIds: new List<string>(),
            Metadata: new Dictionary<string, string>
            {
                ["generatedFrom"] = "SwaggerGrouped",
                ["operationCount"] = operations.Count.ToString(),
                ["resource"] = resourceName
            },
            LastVerified: null,
            RequiredEvidenceLevel: EvidenceLevel.ApiTests
        );
    }

    /// <summary>
    /// Get resource name from operation (heuristic).
    /// Uses first tag, or extracts from path.
    /// </summary>
    private static string GetResourceName(SwaggerOperation operation)
    {
        // Prefer tag
        if (operation.Tags.Count > 0)
        {
            return operation.Tags[0];
        }

        // Extract from path (e.g., /api/team/members -> "team")
        var segments = operation.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            // Skip common prefixes
            if (segment.Equals("api", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip path parameters (e.g., {id})
            if (segment.StartsWith('{') && segment.EndsWith('}'))
                continue;

            return segment;
        }

        return "General";
    }

    /// <summary>
    /// Infer capability status from operation characteristics.
    /// Heuristic: GET operations are typically more stable than POST/DELETE.
    /// </summary>
    private static CapabilityStatus InferStatus(SwaggerOperation operation)
    {
        // Safe read operations: likely more stable
        if (operation.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
            operation.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
        {
            return CapabilityStatus.Planned; // Could be ApiVerified if we validate later
        }

        // Write operations: need more validation
        return CapabilityStatus.Planned;
    }

    /// <summary>
    /// Link test methods to capabilities heuristically.
    /// Matches test method names to operation IDs.
    /// </summary>
    public static void LinkTestsHeuristically(
        CapabilityRegistry registry,
        IReadOnlyList<string> testMethodNames)
    {
        foreach (var capability in registry.GetAllCapabilities())
        {
            var linkedTests = new List<string>();

            foreach (var operationId in capability.ApiOperationIds)
            {
                // Find tests that mention this operation ID
                var matchingTests = testMethodNames
                    .Where(testName => 
                        testName.Contains(operationId, StringComparison.OrdinalIgnoreCase) ||
                        operationId.Contains(testName.Replace("Test", ""), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                linkedTests.AddRange(matchingTests);
            }

            if (linkedTests.Count > 0)
            {
                // Update capability with linked tests
                var updatedCapability = capability with 
                { 
                    ApiTestIds = linkedTests.Distinct().ToList() 
                };
                registry.RegisterCapability(updatedCapability);
            }
        }
    }

    /// <summary>
    /// Calculate test coverage for all capabilities.
    /// Returns a report showing which capabilities have tests.
    /// </summary>
    public static CapabilityCoverageReport CalculateCoverage(CapabilityRegistry registry)
    {
        var allCapabilities = registry.GetAllCapabilities();
        var totalCapabilities = allCapabilities.Count;
        var capabilitiesWithTests = allCapabilities.Count(c => c.ApiTestIds?.Count > 0);
        var totalOperations = allCapabilities.Sum(c => c.ApiOperationIds.Count);
        var totalTests = allCapabilities.Sum(c => c.ApiTestIds?.Count ?? 0);

        var detailsByCapability = allCapabilities
            .Select(c => new CapabilityCoverageDetail
            {
                CapabilityId = c.Id,
                Name = c.Name,
                Category = c.Category,
                Status = c.Status,
                OperationCount = c.ApiOperationIds.Count,
                TestCount = c.ApiTestIds?.Count ?? 0,
                HasTests = (c.ApiTestIds?.Count ?? 0) > 0,
                CoveragePercentage = registry.GetApiTestCoverage(c.Id)
            })
            .OrderByDescending(d => d.TestCount)
            .ThenBy(d => d.CapabilityId)
            .ToList();

        return new CapabilityCoverageReport
        {
            TotalCapabilities = totalCapabilities,
            CapabilitiesWithTests = capabilitiesWithTests,
            CapabilitiesWithoutTests = totalCapabilities - capabilitiesWithTests,
            TotalOperations = totalOperations,
            TotalTests = totalTests,
            OverallCoveragePercentage = totalCapabilities > 0 
                ? (double)capabilitiesWithTests / totalCapabilities * 100 
                : 0,
            Details = detailsByCapability
        };
    }
}

/// <summary>
/// Coverage report for all capabilities.
/// Shows what we know, not what we claim to know.
/// </summary>
public sealed record CapabilityCoverageReport
{
    public int TotalCapabilities { get; init; }
    public int CapabilitiesWithTests { get; init; }
    public int CapabilitiesWithoutTests { get; init; }
    public int TotalOperations { get; init; }
    public int TotalTests { get; init; }
    public double OverallCoveragePercentage { get; init; }
    public IReadOnlyList<CapabilityCoverageDetail> Details { get; init; } = Array.Empty<CapabilityCoverageDetail>();
}

/// <summary>
/// Coverage detail for a single capability.
/// </summary>
public sealed record CapabilityCoverageDetail
{
    public required string CapabilityId { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required CapabilityStatus Status { get; init; }
    public required int OperationCount { get; init; }
    public required int TestCount { get; init; }
    public required bool HasTests { get; init; }
    public required double CoveragePercentage { get; init; }
}
