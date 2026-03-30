namespace ApiFirst.LlmOrchestration.Models;

public sealed record PlannedAction(
    string OperationId,
    IReadOnlyDictionary<string, string?> Arguments,
    string? RequestBodyJson = null);
