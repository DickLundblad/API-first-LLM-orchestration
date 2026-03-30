using System.Text.Json;
using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Configuration;
using ApiFirst.LlmOrchestration.Models;
using ApiFirst.LlmOrchestration.Orchestration;
using ApiFirst.LlmOrchestration.Planning;
using ApiFirst.LlmOrchestration.Providers;
using ApiFirst.LlmOrchestration.Swagger;

namespace ApiFirst.LlmOrchestration.McpServer;

public sealed class McpServer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly McpToolDefinition[] Tools =
    [
        new McpToolDefinition(
            "health",
            "Check whether the MCP server is running.",
            true,
            new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>()
            }),
        new McpToolDefinition(
            "search_operations",
            "Search swagger operations by keyword.",
            true,
            new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["required"] = new[] { "query" },
                ["properties"] = new Dictionary<string, object?>
                {
                    ["query"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["swaggerUrl"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["swaggerFile"] = new Dictionary<string, object?> { ["type"] = "string" }
                }
            }),
        new McpToolDefinition(
            "list_operations",
            "List parsed swagger operations.",
            true,
            new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>
                {
                    ["swaggerUrl"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["swaggerFile"] = new Dictionary<string, object?> { ["type"] = "string" }
                }
            }),
        new McpToolDefinition(
            "run_use_case",
            "Plan and execute a goal against the target API.",
            false,
            new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["required"] = new[] { "goal" },
                ["properties"] = new Dictionary<string, object?>
                {
                    ["userId"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["swaggerUrl"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["swaggerFile"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["apiBaseUrl"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["goal"] = new Dictionary<string, object?> { ["type"] = "string" }
                }
            })
    ];

    private readonly McpServerOptions _options;
    private readonly ISwaggerDocumentLoader _swaggerDocumentLoader;
    private readonly IUserModelSettingsProvider _userModelSettingsProvider;
    private readonly ITextGenerationClientFactory _textGenerationClientFactory;

    private McpServer(
        McpServerOptions options,
        ISwaggerDocumentLoader swaggerDocumentLoader,
        IUserModelSettingsProvider userModelSettingsProvider,
        ITextGenerationClientFactory textGenerationClientFactory)
    {
        _options = options;
        _swaggerDocumentLoader = swaggerDocumentLoader;
        _userModelSettingsProvider = userModelSettingsProvider;
        _textGenerationClientFactory = textGenerationClientFactory;
    }

    public static McpServer CreateDefault(McpServerOptions options)
    {
        return new McpServer(
            options,
            new SwaggerDocumentLoader(),
            new EnvironmentUserModelSettingsProvider(),
            new TextGenerationClientFactory(new HttpClient()));
    }

    public async Task RunAsync(TextReader input, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        var dispatcher = new RequestDispatcher(this);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await input.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in document.RootElement.EnumerateArray())
                    {
                        var response = await dispatcher.DispatchAsync(item, cancellationToken).ConfigureAwait(false);
                        if (response is not null)
                        {
                            await output.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions)).ConfigureAwait(false);
                            await output.FlushAsync().ConfigureAwait(false);
                        }
                    }
                    continue;
                }

                var responseItem = await dispatcher.DispatchAsync(document.RootElement, cancellationToken).ConfigureAwait(false);
                if (responseItem is not null)
                {
                    await output.WriteLineAsync(JsonSerializer.Serialize(responseItem, JsonOptions)).ConfigureAwait(false);
                    await output.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = McpJsonRpcResponse.CreateErrorResponse(null, -32700, ex.Message);
                await output.WriteLineAsync(JsonSerializer.Serialize(errorResponse, JsonOptions)).ConfigureAwait(false);
                await output.FlushAsync().ConfigureAwait(false);
                await error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
            }
        }
    }

    internal IReadOnlyList<McpToolDefinition> GetTools()
    {
        return Tools;
    }

    internal async Task<McpJsonRpcResponse?> DispatchAsync(JsonElement message, CancellationToken cancellationToken)
    {
        if (!message.TryGetProperty("method", out var methodElement))
        {
            return null;
        }

        var method = methodElement.GetString();
        if (string.IsNullOrWhiteSpace(method))
        {
            return null;
        }

        var isNotification = !message.TryGetProperty("id", out var idElement);
        JsonElement? requestId = isNotification ? null : idElement.Clone();

        switch (method)
        {
            case "initialize":
                return McpJsonRpcResponse.CreateResult(requestId, await CreateInitializeResultAsync(message, cancellationToken).ConfigureAwait(false));
            case "initialized":
                return null;
            case "ping":
                return McpJsonRpcResponse.CreateResult(requestId, new Dictionary<string, object?>());
            case "shutdown":
                return McpJsonRpcResponse.CreateResult(requestId, null);
            case "exit":
                return null;
            case "tools/list":
                return McpJsonRpcResponse.CreateResult(requestId, CreateToolsListResult());
            case "tools/call":
                return McpJsonRpcResponse.CreateResult(requestId, await CallToolAsync(message, cancellationToken).ConfigureAwait(false));
            default:
                return McpJsonRpcResponse.CreateError(requestId, -32601, $"Method '{method}' is not supported.");
        }
    }

    private Task<object?> CreateInitializeResultAsync(JsonElement message, CancellationToken cancellationToken)
    {
        var requestedVersion = GetStringProperty(message, "params", "protocolVersion");
        var protocolVersion = ResolveProtocolVersion(requestedVersion);

        var result = new Dictionary<string, object?>
        {
            ["protocolVersion"] = protocolVersion,
            ["serverInfo"] = new Dictionary<string, object?>
            {
                ["name"] = "ApiFirst.LlmOrchestration.McpServer",
                ["version"] = typeof(McpServer).Assembly.GetName().Version?.ToString() ?? "1.0.0"
            },
            ["capabilities"] = new Dictionary<string, object?>
            {
                ["tools"] = new Dictionary<string, object?>()
            }
        };

        return Task.FromResult<object?>(result);
    }

    private object CreateToolsListResult()
    {
        return new Dictionary<string, object?>
        {
            ["tools"] = Tools.Select(tool => new Dictionary<string, object?>
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["inputSchema"] = tool.InputSchema,
                ["annotations"] = new Dictionary<string, object?>
                {
                    ["readOnlyHint"] = tool.ReadOnlyHint
                }
            }).ToArray()
        };
    }

    private async Task<object> CallToolAsync(JsonElement message, CancellationToken cancellationToken)
    {
        try
        {
            var (toolName, arguments) = GetToolCall(message);

            return toolName switch
            {
                "health" => CreateToolResult("ok", new { status = "ok" }),
                "search_operations" => await SearchOperationsAsync(arguments, cancellationToken).ConfigureAwait(false),
                "list_operations" => await ListOperationsAsync(arguments, cancellationToken).ConfigureAwait(false),
                "run_use_case" => await RunUseCaseAsync(arguments, cancellationToken).ConfigureAwait(false),
                _ => CreateToolError($"Unknown tool '{toolName}'.")
            };
        }
        catch (Exception ex)
        {
            return CreateToolError(ex.Message);
        }
    }

    private async Task<object> SearchOperationsAsync(IReadOnlyDictionary<string, string?> arguments, CancellationToken cancellationToken)
    {
        var query = Require(arguments, "query");
        var catalog = await LoadCatalogAsync(arguments, cancellationToken).ConfigureAwait(false);

        var matches = catalog.Operations
            .Where(operation => MatchesQuery(operation, query))
            .Select(operation => new
            {
                operation.OperationId,
                operation.Method,
                operation.Path,
                operation.Summary,
                operation.Tags
            })
            .ToArray();

        var text = matches.Length == 0
            ? $"No operations matched '{query}'."
            : $"Found {matches.Length} matching operation(s) for '{query}'.";

        return CreateToolResult(text, new
        {
            query,
            matches
        });
    }

    private static bool MatchesQuery(SwaggerOperation operation, string query)
    {
        return Contains(operation.OperationId, query)
            || Contains(operation.Summary, query)
            || Contains(operation.Path, query)
            || operation.Tags.Any(tag => Contains(tag, query));
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
    private async Task<object> ListOperationsAsync(IReadOnlyDictionary<string, string?> arguments, CancellationToken cancellationToken)
    {
        var catalog = await LoadCatalogAsync(arguments, cancellationToken).ConfigureAwait(false);
        var text = SwaggerCatalogPrinter.Print(catalog);

        return CreateToolResult(text, new
        {
            catalog.Title,
            catalog.Version,
            catalog.Description,
            catalog.ServerBasePath,
            Operations = catalog.Operations.Select(operation => new
            {
                operation.OperationId,
                operation.Method,
                operation.Path,
                operation.Summary,
                operation.Tags,
                operation.HasRequestBody,
                operation.SecurityRequirements,
                operation.ResponseStatusCodes
            }).ToArray()
        });
    }


    private async Task<object> RunUseCaseAsync(IReadOnlyDictionary<string, string?> arguments, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId(arguments);
        var goal = Require(arguments, "goal");
        var swaggerSource = ResolveSwaggerUrl(arguments) ?? ResolveSwaggerFile(arguments);
        var apiBaseUrl = ResolveApiBaseUrl(arguments, swaggerSource);
        var catalog = await LoadCatalogAsync(arguments, cancellationToken).ConfigureAwait(false);

        var settings = await _userModelSettingsProvider.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        var client = _textGenerationClientFactory.Create(settings);
        var planner = new ValidatingUseCasePlanner(client);
        var executor = new HttpApiExecutor(new HttpClient(), apiBaseUrl, catalog.ServerBasePath);
        var orchestrator = new ApiAgentOrchestrator(_swaggerDocumentLoader, planner, executor);

        var result = await orchestrator.ExecuteAsync(catalog, new UseCaseRequest(goal), cancellationToken).ConfigureAwait(false);

        return CreateToolResult(
            $"Executed plan '{result.Plan.Name}' with {result.Results.Count} result(s).",
            new
            {
                Plan = new
                {
                    result.Plan.Name,
                    result.Plan.Rationale,
                    Actions = result.Plan.Actions.Select(action => new
                    {
                        action.OperationId,
                        action.Arguments,
                        action.RequestBodyJson
                    }).ToArray()
                },
                Results = result.Results.Select(item => new
                {
                    item.OperationId,
                    item.Succeeded,
                    StatusCode = item.StatusCode.HasValue ? (int?)item.StatusCode.Value : null,
                    item.ResponseBody
                }).ToArray()
            });
    }
    private async Task<SwaggerDocumentCatalog> LoadCatalogAsync(IReadOnlyDictionary<string, string?> arguments, CancellationToken cancellationToken)
    {
        var swaggerUrl = ResolveSwaggerUrl(arguments);
        if (swaggerUrl is not null)
        {
            return await _swaggerDocumentLoader.LoadFromUrlAsync(swaggerUrl, cancellationToken).ConfigureAwait(false);
        }

        var swaggerFile = ResolveSwaggerFile(arguments);
        if (swaggerFile is not null)
        {
            return await _swaggerDocumentLoader.LoadFromFileAsync(swaggerFile, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Specify swaggerUrl or swaggerFile either on the tool call or when launching the server.");
    }

    private string? ResolveSwaggerUrl(IReadOnlyDictionary<string, string?> arguments)
    {
        if (arguments.TryGetValue("swaggerUrl", out var explicitValue) && !string.IsNullOrWhiteSpace(explicitValue))
        {
            return explicitValue;
        }

        return _options.DefaultSwaggerUrl;
    }

    private string? ResolveSwaggerFile(IReadOnlyDictionary<string, string?> arguments)
    {
        if (arguments.TryGetValue("swaggerFile", out var explicitValue) && !string.IsNullOrWhiteSpace(explicitValue))
        {
            return explicitValue;
        }

        return _options.DefaultSwaggerFile;
    }

    private Uri ResolveApiBaseUrl(IReadOnlyDictionary<string, string?> arguments, string? swaggerSource)
    {
        if (arguments.TryGetValue("apiBaseUrl", out var explicitValue) && !string.IsNullOrWhiteSpace(explicitValue))
        {
            return new Uri(explicitValue, UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(_options.DefaultApiBaseUrl))
        {
            return new Uri(_options.DefaultApiBaseUrl, UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(swaggerSource) && Uri.TryCreate(swaggerSource, UriKind.Absolute, out var swaggerUri))
        {
            return new Uri(swaggerUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute);
        }

        throw new InvalidOperationException("Specify apiBaseUrl either on the tool call or when launching the server.");
    }

    private string ResolveUserId(IReadOnlyDictionary<string, string?> arguments)
    {
        if (arguments.TryGetValue("userId", out var explicitValue) && !string.IsNullOrWhiteSpace(explicitValue))
        {
            return explicitValue;
        }

        if (!string.IsNullOrWhiteSpace(_options.DefaultUserId))
        {
            return _options.DefaultUserId;
        }

        throw new InvalidOperationException("Specify userId either on the tool call or when launching the server.");
    }

    private static (string ToolName, IReadOnlyDictionary<string, string?> Arguments) GetToolCall(JsonElement message)
    {
        var parameters = message.TryGetProperty("params", out var paramsElement) && paramsElement.ValueKind == JsonValueKind.Object
            ? paramsElement
            : default;

        var toolName = GetStringProperty(parameters, "name");
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new InvalidOperationException("Tool call message is missing 'params.name'.");
        }

        var arguments = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("arguments", out var argumentsElement) && argumentsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in argumentsElement.EnumerateObject())
            {
                arguments[property.Name] = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.ToString();
            }
        }

        return (toolName, arguments);
    }

    private static string ResolveProtocolVersion(string? requestedVersion)
    {
        var supportedVersions = new[]
        {
            "2024-11-05",
            "2025-03-26",
            "2025-06-18",
            "2025-11-25"
        };

        if (!string.IsNullOrWhiteSpace(requestedVersion) && supportedVersions.Contains(requestedVersion, StringComparer.Ordinal))
        {
            return requestedVersion;
        }

        return supportedVersions[^1];
    }

    private static string? GetStringProperty(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var child))
            {
                return null;
            }

            current = child;
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static string Require(IReadOnlyDictionary<string, string?> arguments, string name)
    {
        if (arguments.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Missing required argument '{name}'.");
    }

    private static object CreateToolResult(string text, object structuredContent)
    {
        return new Dictionary<string, object?>
        {
            ["content"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "text",
                    ["text"] = text
                }
            },
            ["structuredContent"] = structuredContent
        };
    }

    private static object CreateToolError(string text)
    {
        return new Dictionary<string, object?>
        {
            ["isError"] = true,
            ["content"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "text",
                    ["text"] = text
                }
            }
        };
    }

    internal sealed record McpToolDefinition(
        string Name,
        string Description,
        bool ReadOnlyHint,
        object InputSchema);

    internal sealed record McpJsonRpcResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("jsonrpc")] string JsonRpc,
        [property: System.Text.Json.Serialization.JsonPropertyName("id")] JsonElement? Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("result")] object? Result = null,
        [property: System.Text.Json.Serialization.JsonPropertyName("error")] McpJsonRpcError? Error = null)
    {
        public static McpJsonRpcResponse CreateResult(JsonElement? id, object? result)
        {
            return new McpJsonRpcResponse("2.0", id, result, null);
        }

        public static McpJsonRpcResponse CreateError(JsonElement? id, int code, string message)
        {
            return new McpJsonRpcResponse("2.0", id, null, new McpJsonRpcError(code, message));
        }

        public static McpJsonRpcResponse CreateErrorResponse(JsonElement? id, int code, string message)
        {
            return CreateError(id, code, message);
        }
    }

    internal sealed record McpJsonRpcError(
        [property: System.Text.Json.Serialization.JsonPropertyName("code")] int Code,
        [property: System.Text.Json.Serialization.JsonPropertyName("message")] string Message);

    internal sealed class RequestDispatcher
    {
        private readonly McpServer _server;

        public RequestDispatcher(McpServer server)
        {
            _server = server;
        }

        public Task<McpJsonRpcResponse?> DispatchAsync(JsonElement message, CancellationToken cancellationToken)
        {
            return _server.DispatchAsync(message, cancellationToken);
        }
    }
}










