namespace ApiFirst.LlmOrchestration.Configuration;

public sealed record UserModelSettings(
    string UserId,
    string Provider,
    string Model,
    string ApiKey,
    Uri? Endpoint = null,
    string? ReasoningEffort = null,
    string? SystemInstructions = null);
