namespace ApiFirst.LlmOrchestration.Models;

public sealed record UseCaseExecutionResult(
    UseCasePlan Plan,
    IReadOnlyList<PlannedActionResult> Results);
