namespace ApiFirst.LlmOrchestration.Planning;

public static class PlannerJsonSchema
{
    public const string Text = """
{
  "type": "object",
  "required": ["name", "rationale", "actions"],
  "properties": {
    "name": { "type": "string" },
    "rationale": { "type": "string" },
    "actions": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["operationId", "arguments"],
        "properties": {
          "operationId": { "type": "string" },
          "arguments": {
            "type": "object",
            "additionalProperties": { "type": ["string", "null"] }
          },
          "requestBodyJson": { "type": ["string", "null"] }
        }
      }
    }
  }
}
""";
}
