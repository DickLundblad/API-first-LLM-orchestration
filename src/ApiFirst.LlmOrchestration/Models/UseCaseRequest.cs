namespace ApiFirst.LlmOrchestration.Models;

public sealed record UseCaseRequest(
    string Objective,
    string? Context = null);
