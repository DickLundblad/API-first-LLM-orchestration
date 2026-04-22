using System.Text.Json;

namespace ApiFirst.LlmOrchestration.Registry;

/// <summary>
/// Manages test mappings from external test suites (e.g., Python tests in another repo).
/// Instead of auto-discovery, tests are registered explicitly via configuration or API.
/// </summary>
public sealed class ExternalTestMapper
{
    /// <summary>
    /// Load test mappings from a JSON configuration file.
    /// Format:
    /// {
    ///   "tests": [
    ///     {
    ///       "testId": "tests/api/test_team.py::test_get_team_member",
    ///       "testName": "Test Get Team Member",
    ///       "operations": ["GetTeamMember"],
    ///       "capabilities": ["getteammember", "team-management"]
    ///     }
    ///   ]
    /// }
    /// </summary>
    public static void LoadFromFile(string filePath, CapabilityRegistry registry)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var config = JsonSerializer.Deserialize<TestMappingConfig>(json, options);
        if (config?.Tests == null)
        {
            return;
        }

        ApplyTestMappings(config.Tests, registry);
    }

    /// <summary>
    /// Load test mappings from test result files (e.g., pytest JSON report).
    /// </summary>
    public static void LoadFromPytestReport(string reportPath, CapabilityRegistry registry)
    {
        if (!File.Exists(reportPath))
        {
            return;
        }

        var json = File.ReadAllText(reportPath);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var report = JsonSerializer.Deserialize<PytestReport>(json, options);
        if (report?.Tests == null)
        {
            return;
        }

        var testMappings = InferMappingsFromPytestTests(report.Tests);
        ApplyTestMappings(testMappings, registry);
    }

    /// <summary>
    /// Apply test mappings to capabilities in the registry.
    /// </summary>
    private static void ApplyTestMappings(List<TestMapping> mappings, CapabilityRegistry registry)
    {
        var testsByCapability = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            if (mapping.Capabilities != null)
            {
                foreach (var capabilityId in mapping.Capabilities)
                {
                    if (!testsByCapability.ContainsKey(capabilityId))
                        testsByCapability[capabilityId] = new List<string>();
                    testsByCapability[capabilityId].Add(mapping.TestId);
                }
            }

            if (mapping.Operations != null)
            {
                foreach (var operationId in mapping.Operations)
                {
                    foreach (var capability in registry.GetCapabilitiesByOperation(operationId))
                    {
                        if (!testsByCapability.ContainsKey(capability.Id))
                            testsByCapability[capability.Id] = new List<string>();
                        if (!testsByCapability[capability.Id].Contains(mapping.TestId))
                            testsByCapability[capability.Id].Add(mapping.TestId);
                    }
                }
            }
        }

        foreach (var kvp in testsByCapability)
        {
            var capability = registry.GetCapability(kvp.Key);
            if (capability == null)
                continue;

            var updatedCapability = capability with
            {
                ApiTestIds = (capability.ApiTestIds ?? new List<string>())
                    .Concat(kvp.Value)
                    .Distinct()
                    .ToList()
            };
            registry.RegisterCapability(updatedCapability);

            foreach (var testId in kvp.Value.Distinct())
            {
                registry.RecordEvidence(new CapabilityEvidence(
                    CapabilityId: capability.Id,
                    Type: EvidenceType.ApiAutomatedTest,
                    Status: EvidenceStatus.Success,
                    Source: EvidenceSource.External,
                    Timestamp: DateTime.UtcNow,
                    Details: $"Test: {testId}"
                ));
            }
        }
    }

    /// <summary>
    /// Infer capability mappings from pytest test names using heuristics.
    /// </summary>
    private static List<TestMapping> InferMappingsFromPytestTests(List<PytestTest> tests)
    {
        var mappings = new List<TestMapping>();

        foreach (var test in tests)
        {
            var testId = test.NodeId ?? test.Name ?? "";
            var operations = InferOperationsFromTestName(testId);

            mappings.Add(new TestMapping
            {
                TestId = testId,
                TestName = test.Name ?? testId,
                Operations = operations,
                Capabilities = null // Will be inferred from operations
            });
        }

        return mappings;
    }

    /// <summary>
    /// Infer operation IDs from pytest test names.
    /// Examples:
    /// - test_get_team_member -> GetTeamMember
    /// - test_update_team_member_success -> UpdateTeamMember
    /// </summary>
    private static List<string> InferOperationsFromTestName(string testName)
    {
        var operations = new List<string>();

        // Remove common prefixes/suffixes
        var cleaned = testName
            .Replace("test_", "")
            .Replace("_success", "")
            .Replace("_failure", "")
            .Replace("_error", "")
            .Replace("::test_", "_");

        // Convert snake_case to PascalCase
        var parts = cleaned.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            var pascalCase = string.Join("", parts.Select(p => 
                char.ToUpper(p[0]) + p.Substring(1).ToLower()));
            operations.Add(pascalCase);
        }

        return operations;
    }

    /// <summary>
    /// Generate a template test mapping file for manual configuration.
    /// </summary>
    public static void GenerateTemplateFile(string filePath, CapabilityRegistry registry)
    {
        var template = new TestMappingConfig
        {
            Tests = new List<TestMapping>()
        };

        // Create example mappings for each capability
        foreach (var capability in registry.GetAllCapabilities().Take(5))
        {
            template.Tests.Add(new TestMapping
            {
                TestId = $"tests/api/test_{capability.Category.ToLower()}.py::test_{capability.Id}",
                TestName = $"Test {capability.Name}",
                Operations = capability.ApiOperationIds.ToList(),
                Capabilities = new List<string> { capability.Id }
            });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(template, options);
        File.WriteAllText(filePath, json);
    }
}

// Configuration models

public sealed class TestMappingConfig
{
    public List<TestMapping>? Tests { get; set; }
}

public sealed class TestMapping
{
    public required string TestId { get; set; }
    public string? TestName { get; set; }
    public List<string>? Operations { get; set; }
    public List<string>? Capabilities { get; set; }
}

public sealed class PytestReport
{
    public List<PytestTest>? Tests { get; set; }
}

public sealed class PytestTest
{
    public string? NodeId { get; set; }
    public string? Name { get; set; }
    public string? Outcome { get; set; }
}
