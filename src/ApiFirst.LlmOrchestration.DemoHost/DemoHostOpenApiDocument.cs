namespace ApiFirst.LlmOrchestration.DemoHost;

public static class DemoHostOpenApiDocument
{
    public static string Build(string baseUrl)
    {
        return $$"""
        {
          "openapi": "3.0.0",
          "info": {
            "title": "API-first LLM Orchestration Demo Host",
            "version": "1.0.0",
            "description": "Local demo host for Copilot integrations."
          },
          "servers": [
            { "url": "{{baseUrl}}" }
          ],
          "paths": {
            "/health": {
              "get": {
                "operationId": "GetHealth",
                "summary": "Check whether the demo host is running",
                "responses": {
                  "200": { "description": "Host is healthy" }
                }
              }
            },
            "/operations": {
              "get": {
                "operationId": "ListOperations",
                "summary": "List parsed operations from a swagger document",
                "parameters": [
                  { "name": "swaggerUrl", "in": "query", "required": false, "schema": { "type": "string" } },
                  { "name": "swaggerFile", "in": "query", "required": false, "schema": { "type": "string" } }
                ],
                "responses": {
                  "200": { "description": "Operation list" }
                }
              }
            },
            "/run": {
              "post": {
                "operationId": "RunUseCase",
                "summary": "Plan and execute a goal against the target API",
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": {
                        "type": "object",
                        "required": ["userId", "goal"],
                        "properties": {
                          "userId": { "type": "string" },
                          "swaggerUrl": { "type": "string" },
                          "swaggerFile": { "type": "string" },
                          "goal": { "type": "string" },
                          "apiBaseUrl": { "type": "string" }
                        }
                      }
                    }
                  }
                },
                "responses": {
                  "200": { "description": "Plan execution result" }
                }
              }
            }
          }
        }
        """;
    }
}
