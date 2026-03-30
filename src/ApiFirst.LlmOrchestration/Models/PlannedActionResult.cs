using System.Net;

namespace ApiFirst.LlmOrchestration.Models;

public sealed record PlannedActionResult(
    string OperationId,
    bool Succeeded,
    HttpStatusCode? StatusCode,
    string? ResponseBody);
