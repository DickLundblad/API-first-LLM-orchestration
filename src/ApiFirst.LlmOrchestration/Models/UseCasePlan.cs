namespace ApiFirst.LlmOrchestration.Models;

public sealed record UseCasePlan(
    string Name,
    string Rationale,
    IReadOnlyList<PlannedAction> Actions);
