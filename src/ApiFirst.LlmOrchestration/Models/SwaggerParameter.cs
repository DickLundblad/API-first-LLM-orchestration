namespace ApiFirst.LlmOrchestration.Models;

public sealed record SwaggerParameter(
    string Name,
    string Location,
    bool Required,
    string? SchemaType);
