using ApiFirst.LlmOrchestration.Registry;

namespace ApiFirst.LlmOrchestration.Tests.Examples;

/// <summary>
/// Example: How to integrate test results with the Capability Registry.
/// This shows how to record evidence from automated tests.
/// API-first approach: Emphasizes API tests as primary evidence.
/// </summary>
public class CapabilityRegistryTestIntegration
{
    private readonly CapabilityRegistry _registry;

    public CapabilityRegistryTestIntegration()
    {
        // Load registry (in real scenario, inject via DI)
        _registry = new CapabilityRegistry();
    }

    /// <summary>
    /// Example: Report successful API test execution as evidence.
    /// </summary>
    public void ReportApiTestSuccess(string capabilityId, string testId)
    {
        _registry.RecordEvidence(new CapabilityEvidence(
            capabilityId,
            EvidenceType.ApiAutomatedTest, // API test - PRIMARY evidence
            EvidenceStatus.Success,
            EvidenceSource.LocalTestRunner,
            DateTime.UtcNow,
            $"API test {testId} passed",
            new Dictionary<string, object>
            {
                ["testId"] = testId,
                ["framework"] = "xUnit",
                ["testType"] = "API"
            }));
    }

    /// <summary>
    /// Example: Report failed API test execution.
    /// </summary>
    public void ReportApiTestFailure(string capabilityId, string testId, string errorMessage)
    {
        _registry.RecordEvidence(new CapabilityEvidence(
            capabilityId,
            EvidenceType.ApiAutomatedTest,
            EvidenceStatus.Failed,
            EvidenceSource.LocalTestRunner,
            DateTime.UtcNow,
            $"API test {testId} failed: {errorMessage}",
            new Dictionary<string, object>
            {
                ["testId"] = testId,
                ["error"] = errorMessage,
                ["framework"] = "xUnit",
                ["testType"] = "API"
            }));
    }

    /// <summary>
    /// Example: Report GUI test result (TERTIARY evidence, only if GUI exists).
    /// </summary>
    public void ReportGuiTestResult(string capabilityId, string testId, bool success, string? errorMessage = null)
    {
        _registry.RecordEvidence(new CapabilityEvidence(
            capabilityId,
            EvidenceType.GuiAutomatedTest,
            success ? EvidenceStatus.Success : EvidenceStatus.Failed,
            EvidenceSource.LocalTestRunner,
            DateTime.UtcNow,
            success ? $"GUI test {testId} passed" : $"GUI test {testId} failed: {errorMessage}",
            new Dictionary<string, object>
            {
                ["testId"] = testId,
                ["framework"] = "Playwright/Selenium",
                ["testType"] = "GUI"
            }));
    }

    /// <summary>
    /// Example: Use as a test base class utility method.
    /// </summary>
    public static void RecordTestResult(
        CapabilityRegistry registry, 
        string capabilityId, 
        bool success, 
        string testName,
        EvidenceType testType = EvidenceType.ApiAutomatedTest)
    {
        registry.RecordEvidence(new CapabilityEvidence(
            capabilityId,
            testType,
            success ? EvidenceStatus.Success : EvidenceStatus.Failed,
            EvidenceSource.LocalTestRunner,
            DateTime.UtcNow,
            $"Test {testName} {(success ? "passed" : "failed")}"
        ));
    }
}

/// <summary>
/// Example: Custom test attribute to automatically record evidence.
/// Usage: [CapabilityTest("team-member-management")]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CapabilityTestAttribute : Attribute
{
    public string CapabilityId { get; }
    public EvidenceType TestType { get; }

    public CapabilityTestAttribute(string capabilityId, EvidenceType testType = EvidenceType.ApiAutomatedTest)
    {
        CapabilityId = capabilityId;
        TestType = testType;
    }
}

/// <summary>
/// Example test class showing integration with capability registry.
/// API-first: Focus on API tests as primary evidence.
/// </summary>
public class TeamMemberManagementTests
{
    private readonly CapabilityRegistry _registry;

    public TeamMemberManagementTests()
    {
        // In a real scenario, inject via constructor
        var registryPath = Path.Combine(AppContext.BaseDirectory, "CapabilityRegistry.json");
        _registry = File.Exists(registryPath)
            ? CapabilityRegistry.LoadFromFile(registryPath)
            : new CapabilityRegistry();
    }

    // [Fact]
    [CapabilityTest("team-member-management", EvidenceType.ApiAutomatedTest)]
    public void GetTeamMembers_ReturnsAllMembers()
    {
        // Arrange
        var capabilityId = "team-member-management";

        try
        {
            // Act
            // ... your API test logic here ...
            var success = true; // Replace with actual test result

            // Assert & Record
            if (success)
            {
                _registry.RecordEvidence(new CapabilityEvidence(
                    capabilityId,
                    EvidenceType.ApiAutomatedTest,
                    EvidenceStatus.Success,
                    EvidenceSource.LocalTestRunner,
                    DateTime.UtcNow,
                    "API: GetTeamMembers returns all team members successfully"));
            }
        }
        catch (Exception ex)
        {
            _registry.RecordEvidence(new CapabilityEvidence(
                capabilityId,
                EvidenceType.ApiAutomatedTest,
                EvidenceStatus.Failed,
                EvidenceSource.LocalTestRunner,
                DateTime.UtcNow,
                $"API: GetTeamMembers failed: {ex.Message}"));
            throw;
        }
    }

    // [Fact]
    [CapabilityTest("team-member-management", EvidenceType.ApiAutomatedTest)]
    public void UpdateTeamMember_UpdatesSuccessfully()
    {
        var capabilityId = "team-member-management";

        try
        {
            // ... API test logic ...
            var success = true;

            if (success)
            {
                _registry.RecordEvidence(new CapabilityEvidence(
                    capabilityId,
                    EvidenceType.ApiAutomatedTest,
                    EvidenceStatus.Success,
                    EvidenceSource.LocalTestRunner,
                    DateTime.UtcNow,
                    "API: UpdateTeamMember updates team member successfully"));
            }
        }
        catch (Exception ex)
        {
            _registry.RecordEvidence(new CapabilityEvidence(
                capabilityId,
                EvidenceType.ApiAutomatedTest,
                EvidenceStatus.Failed,
                EvidenceSource.LocalTestRunner,
                DateTime.UtcNow,
                $"API: UpdateTeamMember failed: {ex.Message}"));
            throw;
        }
    }

    // [Fact]
    [CapabilityTest("team-member-management", EvidenceType.GuiAutomatedTest)]
    public void TeamMemberGui_CanEditProfile()
    {
        var capabilityId = "team-member-management";

        try
        {
            // ... GUI test logic (Playwright/Selenium) ...
            var success = true;

            if (success)
            {
                _registry.RecordEvidence(new CapabilityEvidence(
                    capabilityId,
                    EvidenceType.GuiAutomatedTest,
                    EvidenceStatus.Success,
                    EvidenceSource.LocalTestRunner,
                    DateTime.UtcNow,
                    "GUI: Can edit team member profile via /team/{id} route"));
            }
        }
        catch (Exception ex)
        {
            _registry.RecordEvidence(new CapabilityEvidence(
                capabilityId,
                EvidenceType.GuiAutomatedTest,
                EvidenceStatus.Failed,
                EvidenceSource.LocalTestRunner,
                DateTime.UtcNow,
                $"GUI: Edit profile failed: {ex.Message}"));
            throw;
        }
    }
}

/// <summary>
/// Example: xUnit test collection fixture for automatic evidence recording.
/// </summary>
public class CapabilityRegistryFixture : IDisposable
{
    public CapabilityRegistry Registry { get; }
    private readonly string _registryPath;

    public CapabilityRegistryFixture()
    {
        _registryPath = Path.Combine(AppContext.BaseDirectory, "CapabilityRegistry.json");
        Registry = File.Exists(_registryPath)
            ? CapabilityRegistry.LoadFromFile(_registryPath)
            : new CapabilityRegistry();
    }

    public void Dispose()
    {
        // Optionally save registry after test run
        // Registry.SaveToFile(_registryPath);
    }
}

/// <summary>
/// Example: How to use the fixture in tests.
/// </summary>
// [Collection("CapabilityRegistry")]
public class ExampleTestsWithFixture
{
    private readonly CapabilityRegistryFixture _fixture;

    public ExampleTestsWithFixture(CapabilityRegistryFixture fixture)
    {
        _fixture = fixture;
    }

    // [Fact]
    public void ExampleApiTest()
    {
        var capabilityId = "team-member-management";

        try
        {
            // ... API test logic ...

            _fixture.Registry.RecordEvidence(new CapabilityEvidence(
                capabilityId,
                EvidenceType.ApiAutomatedTest,
                EvidenceStatus.Success,
                EvidenceSource.LocalTestRunner,
                DateTime.UtcNow,
                "API: ExampleApiTest passed"));
        }
        catch (Exception ex)
        {
            _fixture.Registry.RecordEvidence(new CapabilityEvidence(
                capabilityId,
                EvidenceType.ApiAutomatedTest,
                EvidenceStatus.Failed,
                EvidenceSource.LocalTestRunner,
                DateTime.UtcNow,
                $"API: ExampleApiTest failed: {ex.Message}"));
            throw;
        }
    }

    // [Fact]
    public void ExampleIntegrationTest()
    {
        var capabilityId = "course-enrollment";

        try
        {
            // ... Integration test logic (API + database/external systems) ...

            _fixture.Registry.RecordEvidence(new CapabilityEvidence(
                capabilityId,
                EvidenceType.IntegrationTest,
                EvidenceStatus.Success,
                EvidenceSource.LocalTestRunner,
                DateTime.UtcNow,
                "Integration: EnrollCourse end-to-end flow works",
                new Dictionary<string, object>
                {
                    ["testScope"] = "API + Database + Email notification"
                }));
        }
        catch (Exception ex)
        {
            _fixture.Registry.RecordEvidence(new CapabilityEvidence(
                capabilityId,
                EvidenceType.IntegrationTest,
                EvidenceStatus.Failed,
                EvidenceSource.LocalTestRunner,
                DateTime.UtcNow,
                $"Integration: EnrollCourse failed: {ex.Message}"));
            throw;
        }
    }
}
