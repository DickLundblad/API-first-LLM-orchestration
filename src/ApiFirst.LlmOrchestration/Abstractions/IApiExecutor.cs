using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Abstractions;

public interface IApiExecutor
{
    Task<PlannedActionResult> ExecuteAsync(
        SwaggerOperation operation,
        PlannedAction action,
        CancellationToken cancellationToken = default);
}
