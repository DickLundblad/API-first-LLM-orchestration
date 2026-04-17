namespace ApiFirst.LlmOrchestration.Registry;

/// <summary>
/// Represents a use case capability that the system claims to support.
/// API-first: Core is API operations, GUI and other layers are optional enhancements.
/// A capability links API operations (required) with tests (critical), GUI (optional), and backlog.
/// </summary>
public sealed record UseCaseCapability(
    string Id,
    string Name,
    string Description,
    string Category,
    CapabilityStatus Status,
    IReadOnlyList<string> ApiOperationIds,
    IReadOnlyList<string>? ApiTestIds = null,
    string? GuiRoute = null,
    string? GuiFeature = null,
    IReadOnlyList<string>? GuiTestIds = null,
    IReadOnlyList<string>? BacklogItemIds = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTime? LastVerified = null,
    EvidenceLevel RequiredEvidenceLevel = EvidenceLevel.ApiTests);

/// <summary>
/// Status of a capability in the system.
/// </summary>
public enum CapabilityStatus
{
    /// <summary>Planned but not implemented</summary>
    Planned,

    /// <summary>Implementation in progress</summary>
    InProgress,

    /// <summary>API implemented, awaiting tests</summary>
    ApiImplemented,

    /// <summary>API verified with automated tests</summary>
    ApiVerified,

    /// <summary>Fully verified (API + GUI if applicable)</summary>
    FullyVerified,

    /// <summary>Deprecated or scheduled for removal</summary>
    Deprecated
}

/// <summary>
/// Level of evidence required to consider a capability verified.
/// API-first: API evidence is primary, GUI evidence is secondary.
/// </summary>
public enum EvidenceLevel
{
    /// <summary>Only API execution evidence required</summary>
    ApiExecution,

    /// <summary>API automated tests required (recommended minimum)</summary>
    ApiTests,

    /// <summary>API tests + GUI tests if GUI exists</summary>
    ApiAndGuiTests,

    /// <summary>Full verification including performance benchmarks</summary>
    Comprehensive
}

/// <summary>
/// Evidence that a capability actually works at runtime.
/// Includes source information and importance level.
/// </summary>
public sealed record CapabilityEvidence(
    string CapabilityId,
    EvidenceType Type,
    EvidenceStatus Status,
    EvidenceSource Source,
    DateTime Timestamp,
    string? Details = null,
    IReadOnlyDictionary<string, object>? Data = null);

/// <summary>
/// Type of evidence for capability verification.
/// Ordered by importance: API-first approach prioritizes API evidence.
/// </summary>
public enum EvidenceType
{
    /// <summary>API automated test execution (PRIMARY - most important)</summary>
    ApiAutomatedTest,

    /// <summary>API runtime execution (SECONDARY - proves it works now)</summary>
    ApiExecution,

    /// <summary>GUI automated test execution (TERTIARY - if GUI exists)</summary>
    GuiAutomatedTest,

    /// <summary>Integration test execution (IMPORTANT - cross-system)</summary>
    IntegrationTest,

    /// <summary>Performance benchmark (OPTIONAL - for critical paths)</summary>
    PerformanceBenchmark,

    /// <summary>GUI screenshot captured (DOCUMENTATION - not verification)</summary>
    GuiScreenshot,

    /// <summary>Manual verification (FALLBACK - use sparingly)</summary>
    ManualVerification
}

/// <summary>
/// Source of evidence - where it came from.
/// Helps with traceability and trust level.
/// </summary>
public enum EvidenceSource
{
    /// <summary>From CI/CD pipeline - highest trust</summary>
    CiCdPipeline,

    /// <summary>From local test runner</summary>
    LocalTestRunner,

    /// <summary>From runtime validator</summary>
    RuntimeValidator,

    /// <summary>From automated monitoring</summary>
    AutomatedMonitoring,

    /// <summary>From manual testing</summary>
    ManualTesting,

    /// <summary>From external source</summary>
    External
}

/// <summary>
/// Status of capability evidence.
/// </summary>
public enum EvidenceStatus
{
    Success,
    Failed,
    Inconclusive,
    NotApplicable
}
