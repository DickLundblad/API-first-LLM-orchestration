using System.Text.Json;
using ApiFirst.LlmOrchestration.McpServer;
using ApiFirst.LlmOrchestration.Tests;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests.Mcp;

public sealed class McpServerProtocolTests
{
    [Test]
    public async Task Initialize_and_tools_list_return_mcp_shapes()
    {
        var server = McpServer.CreateDefault(new McpServerOptions(false, null, null, null, null, null));
        var output = await RunAsync(server, CreateInitializeMessage() + Environment.NewLine + CreateToolsListMessage()).ConfigureAwait(false);
        var responses = ParseResponses(output);

        Assert.That(responses, Has.Count.EqualTo(2));
        Assert.That(GetString(responses[0], "result", "protocolVersion"), Is.EqualTo("2024-11-05").Or.EqualTo("2025-03-26").Or.EqualTo("2025-06-18").Or.EqualTo("2025-11-25"));

        var tools = GetArray(responses[1], "result", "tools");
        Assert.That(tools.EnumerateArray().Count(), Is.EqualTo(5));
        var toolNames = tools.EnumerateArray().Select(tool => GetString(tool, "name")).ToArray();
        Assert.That(toolNames, Does.Contain("search_operations"));
        Assert.That(toolNames, Does.Contain("run_use_case"));
        Assert.That(toolNames, Does.Contain("login"));
    }

    [Test]
    public async Task Tools_call_health_returns_ok()
    {
        var server = McpServer.CreateDefault(new McpServerOptions(false, null, null, null, null, null));
        var input = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "health",
                arguments = new { }
            }
        });

        var output = await RunAsync(server, input).ConfigureAwait(false);
        var response = ParseResponses(output).Single();

        Assert.That(GetString(response, "result", "content", 0, "text"), Is.EqualTo("ok"));
        Assert.That(GetString(response, "result", "structuredContent", "status"), Is.EqualTo("ok"));
    }
    [Test]
    public async Task Tools_call_search_operations_returns_matches()
    {
        var swaggerPath = TestSwaggerFile.CreateSync("""
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Demo API",
            "version": "1.0.0"
          },
          "servers": [
            { "url": "https://api.example.test" }
          ],
          "paths": {
            "/team": {
              "get": {
                "operationId": "GetTeamMembers",
                "summary": "Get team members",
                "responses": {
                  "200": { "description": "OK" }
                }
              }
            },
            "/courses": {
              "get": {
                "operationId": "GetCourses",
                "summary": "Get courses",
                "responses": {
                  "200": { "description": "OK" }
                }
              }
            }
          }
        }
        """);

        try
        {
            var server = McpServer.CreateDefault(new McpServerOptions(false, null, null, null, null, null));
            var input = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 4,
                method = "tools/call",
                @params = new
                {
                    name = "search_operations",
                    arguments = new { query = "team", swaggerFile = swaggerPath }
                }
            });

            var output = await RunAsync(server, input).ConfigureAwait(false);
            var response = ParseResponses(output).Single();

            Assert.That(GetString(response, "result", "content", 0, "text"), Does.Contain("Found 1"));
            var matches = GetArray(response, "result", "structuredContent", "matches");
            Assert.That(matches.EnumerateArray().Count(), Is.EqualTo(1));
            Assert.That(GetString(matches.EnumerateArray().Single(), "operationId"), Is.EqualTo("GetTeamMembers"));
        }
        finally
        {
            if (File.Exists(swaggerPath))
            {
                File.Delete(swaggerPath);
            }
        }
    }

    [Test]
    public async Task Tools_call_list_operations_reads_swagger_file()
    {
        var swaggerPath = TestSwaggerFile.CreateSync("""
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Demo API",
            "version": "1.0.0"
          },
          "servers": [
            { "url": "https://api.example.test" }
          ],
          "paths": {
            "/team": {
              "get": {
                "operationId": "GetTeamMembers",
                "summary": "Get team members",
                "responses": {
                  "200": { "description": "OK" }
                }
              }
            }
          }
        }
        """);

        try
        {
            var server = McpServer.CreateDefault(new McpServerOptions(false, null, null, null, null, null));
            var input = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 3,
                method = "tools/call",
                @params = new
                {
                    name = "list_operations",
                    arguments = new { swaggerFile = swaggerPath }
                }
            });

            var output = await RunAsync(server, input).ConfigureAwait(false);
            var response = ParseResponses(output).Single();

            Assert.That(GetString(response, "result", "content", 0, "text"), Does.Contain("GetTeamMembers"));
            var operations = GetArray(response, "result", "structuredContent", "operations");
            Assert.That(operations.EnumerateArray().Count(), Is.EqualTo(1));
        }
        finally
        {
            if (File.Exists(swaggerPath))
            {
                File.Delete(swaggerPath);
            }
        }
    }

    private static string CreateInitializeMessage()
    {
        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "tests", version = "1.0.0" }
            }
        });
    }

    private static string CreateToolsListMessage()
    {
        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        });
    }

    private static async Task<string> RunAsync(McpServer server, string input)
    {
        var reader = new StringReader(input);
        var writer = new StringWriter();
        await server.RunAsync(reader, writer, TextWriter.Null, CancellationToken.None).ConfigureAwait(false);
        return writer.ToString();
    }

    private static List<JsonElement> ParseResponses(string output)
    {
        return output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())
            .ToList();
    }

    private static JsonElement GetArray(JsonElement element, params object[] path)
    {
        var current = Navigate(element, path);
        Assert.That(current.ValueKind, Is.EqualTo(JsonValueKind.Array));
        return current;
    }

    private static string GetString(JsonElement element, params object[] path)
    {
        var current = Navigate(element, path);
        Assert.That(current.ValueKind, Is.EqualTo(JsonValueKind.String));
        return current.GetString()!;
    }

    private static JsonElement Navigate(JsonElement element, IReadOnlyList<object> path)
    {
        var current = element;
        foreach (var part in path)
        {
            if (part is string propertyName)
            {
                Assert.That(current.ValueKind, Is.EqualTo(JsonValueKind.Object));
                Assert.That(current.TryGetProperty(propertyName, out var next), Is.True, $"Missing property '{propertyName}'.");
                current = next;
                continue;
            }

            if (part is int index)
            {
                Assert.That(current.ValueKind, Is.EqualTo(JsonValueKind.Array));
                current = current.EnumerateArray().ElementAt(index);
                continue;
            }

            throw new ArgumentOutOfRangeException(nameof(path), $"Unsupported path segment '{part}'.");
        }

        return current;
    }
}
