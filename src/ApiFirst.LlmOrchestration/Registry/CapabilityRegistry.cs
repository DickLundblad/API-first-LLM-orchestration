using ApiFirst.LlmOrchestration.Models;
using System.Text.Json;

namespace ApiFirst.LlmOrchestration.Registry;

/// <summary>
/// Central registry of all system use case capabilities.
/// API-first: Indexes what the system claims to support, with API operations as the core.
/// This is a prepared/indexed view - not discovery, but declaration of known capabilities.
/// </summary>
public sealed class CapabilityRegistry
{
    private readonly Dictionary<string, UseCaseCapability> _capabilitiesById;
    private readonly Dictionary<string, List<UseCaseCapability>> _capabilitiesByOperation;
    private readonly List<CapabilityEvidence> _evidenceLog;

    public CapabilityRegistry()
    {
        _capabilitiesById = new Dictionary<string, UseCaseCapability>(StringComparer.OrdinalIgnoreCase);
        _capabilitiesByOperation = new Dictionary<string, List<UseCaseCapability>>(StringComparer.OrdinalIgnoreCase);
        _evidenceLog = new List<CapabilityEvidence>();
    }

    /// <summary>
    /// Register a capability in the registry.
    /// </summary>
    public void RegisterCapability(UseCaseCapability capability)
    {
        _capabilitiesById[capability.Id] = capability;

        foreach (var operationId in capability.ApiOperationIds)
        {
            if (!_capabilitiesByOperation.ContainsKey(operationId))
            {
                _capabilitiesByOperation[operationId] = new List<UseCaseCapability>();
            }
            _capabilitiesByOperation[operationId].Add(capability);
        }
    }

    /// <summary>
    /// Get capability by ID.
    /// </summary>
    public UseCaseCapability? GetCapability(string id)
    {
        return _capabilitiesById.TryGetValue(id, out var capability) ? capability : null;
    }

    /// <summary>
    /// Get all capabilities that use a specific API operation.
    /// </summary>
    public IReadOnlyList<UseCaseCapability> GetCapabilitiesByOperation(string operationId)
    {
        return _capabilitiesByOperation.TryGetValue(operationId, out var capabilities) 
            ? capabilities 
            : Array.Empty<UseCaseCapability>();
    }

    /// <summary>
    /// Get all capabilities in a specific category.
    /// </summary>
    public IReadOnlyList<UseCaseCapability> GetCapabilitiesByCategory(string category)
    {
        return _capabilitiesById.Values
            .Where(c => string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Get all capabilities with a specific status.
    /// </summary>
    public IReadOnlyList<UseCaseCapability> GetCapabilitiesByStatus(CapabilityStatus status)
    {
        return _capabilitiesById.Values
            .Where(c => c.Status == status)
            .ToList();
    }

    /// <summary>
    /// Get all registered capabilities.
    /// </summary>
    public IReadOnlyList<UseCaseCapability> GetAllCapabilities()
    {
        return _capabilitiesById.Values.ToList();
    }

    /// <summary>
    /// Record evidence for a capability.
    /// Updates LastVerified timestamp based on evidence quality and capability requirements.
    /// </summary>
    public void RecordEvidence(CapabilityEvidence evidence)
    {
        _evidenceLog.Add(evidence);

        // Update LastVerified only for successful evidence that meets capability requirements
        if (evidence.Status == EvidenceStatus.Success && 
            _capabilitiesById.TryGetValue(evidence.CapabilityId, out var capability))
        {
            // Check if this evidence type meets the capability's required level
            var meetsRequirement = evidence.Type switch
            {
                EvidenceType.ApiAutomatedTest => true, // Always important
                EvidenceType.ApiExecution when capability.RequiredEvidenceLevel <= EvidenceLevel.ApiExecution => true,
                EvidenceType.IntegrationTest => true, // Always important
                EvidenceType.GuiAutomatedTest when capability.GuiRoute != null && 
                    capability.RequiredEvidenceLevel >= EvidenceLevel.ApiAndGuiTests => true,
                EvidenceType.PerformanceBenchmark when capability.RequiredEvidenceLevel >= EvidenceLevel.Comprehensive => true,
                _ => false
            };

            if (meetsRequirement)
            {
                var updatedCapability = capability with { LastVerified = evidence.Timestamp };
                _capabilitiesById[evidence.CapabilityId] = updatedCapability;
            }
        }
    }

    /// <summary>
    /// Get all evidence for a capability.
    /// </summary>
    public IReadOnlyList<CapabilityEvidence> GetEvidence(string capabilityId)
    {
        return _evidenceLog
            .Where(e => string.Equals(e.CapabilityId, capabilityId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Get latest evidence for a capability, optionally filtered by type.
    /// </summary>
    public CapabilityEvidence? GetLatestEvidence(string capabilityId, EvidenceType? type = null)
    {
        var query = _evidenceLog
            .Where(e => string.Equals(e.CapabilityId, capabilityId, StringComparison.OrdinalIgnoreCase));

        if (type.HasValue)
        {
            query = query.Where(e => e.Type == type.Value);
        }

        return query
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefault();
    }

    /// <summary>
    /// Get API test coverage for a capability.
    /// Returns percentage of API operations covered by tests.
    /// </summary>
    public double GetApiTestCoverage(string capabilityId)
    {
        var capability = GetCapability(capabilityId);
        if (capability == null || capability.ApiOperationIds.Count == 0)
            return 0;

        if (capability.ApiTestIds == null || capability.ApiTestIds.Count == 0)
            return 0;

        // In a real implementation, you'd check which operations are covered by which tests
        // For now, we assume each test covers one operation
        var coveredOperations = Math.Min(capability.ApiTestIds.Count, capability.ApiOperationIds.Count);
        return (double)coveredOperations / capability.ApiOperationIds.Count * 100;
    }

    /// <summary>
    /// Check if capability meets its required evidence level.
    /// API-first: Prioritizes API test evidence.
    /// </summary>
    public bool MeetsEvidenceRequirement(string capabilityId)
    {
        var capability = GetCapability(capabilityId);
        if (capability == null)
            return false;

        var evidence = GetEvidence(capabilityId);
        var successfulEvidence = evidence.Where(e => e.Status == EvidenceStatus.Success).ToList();

        return capability.RequiredEvidenceLevel switch
        {
            EvidenceLevel.ApiExecution => 
                successfulEvidence.Any(e => e.Type == EvidenceType.ApiExecution),

            EvidenceLevel.ApiTests => 
                successfulEvidence.Any(e => e.Type == EvidenceType.ApiAutomatedTest),

            EvidenceLevel.ApiAndGuiTests => 
                successfulEvidence.Any(e => e.Type == EvidenceType.ApiAutomatedTest) &&
                (capability.GuiRoute == null || successfulEvidence.Any(e => e.Type == EvidenceType.GuiAutomatedTest)),

            EvidenceLevel.Comprehensive => 
                successfulEvidence.Any(e => e.Type == EvidenceType.ApiAutomatedTest) &&
                (capability.GuiRoute == null || successfulEvidence.Any(e => e.Type == EvidenceType.GuiAutomatedTest)) &&
                successfulEvidence.Any(e => e.Type == EvidenceType.PerformanceBenchmark),

            _ => false
        };
    }

    /// <summary>
    /// Load capabilities from a JSON file.
    /// </summary>
    public static CapabilityRegistry LoadFromFile(string filePath)
    {
        var registry = new CapabilityRegistry();

        if (!File.Exists(filePath))
        {
            return registry;
        }

        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var capabilities = JsonSerializer.Deserialize<List<UseCaseCapability>>(json, options);
        if (capabilities != null)
        {
            foreach (var capability in capabilities)
            {
                registry.RegisterCapability(capability);
            }
        }

        return registry;
    }

    /// <summary>
    /// Save capabilities to a JSON file.
    /// </summary>
    public void SaveToFile(string filePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(_capabilitiesById.Values.ToList(), options);
        File.WriteAllText(filePath, json);
    }
}
