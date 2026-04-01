using System.Collections.Concurrent;
using System.Net;
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
            "login",
            "Authenticate a user and store a session cookie for later API calls.",
            false,
            new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["required"] = new[] { "username", "password" },
                ["properties"] = new Dictionary<string, object?>
                {
                    ["userId"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["swaggerUrl"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["swaggerFile"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["apiBaseUrl"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["username"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["password"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["requestBodyJson"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["loginOperationId"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["loginPath"] = new Dictionary<string, object?> { ["type"] = "string" }
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
                    ["goal"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["planJson"] = new Dictionary<string, object?> { ["type"] = "string" }
                }
            })
    ];

    private readonly McpServerOptions _options;
    private readonly ISwaggerDocumentLoader _swaggerDocumentLoader;
    private readonly IUserModelSettingsProvider _userModelSettingsProvider;
    private readonly ITextGenerationClientFactory _textGenerationClientFactory;
    private readonly ConcurrentDictionary<string, ApiSession> _apiSessions = new(StringComparer.OrdinalIgnoreCase);

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
        // Load default swagger URL from appsettings.json if not set
        if (string.IsNullOrWhiteSpace(options.DefaultSwaggerUrl))
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                using var stream = File.OpenRead(configPath);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("DefaultSwaggerUrl", out var urlProp))
                {
                    options = options with { DefaultSwaggerUrl = urlProp.GetString() };
                }
            }
        }
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
                "login" => await LoginAsync(arguments, cancellationToken).ConfigureAwait(false),
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


    private async Task<object> LoginAsync(IReadOnlyDictionary<string, string?> arguments, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId(arguments);
        var swaggerSource = ResolveSwaggerUrl(arguments) ?? ResolveSwaggerFile(arguments);
        var apiBaseUrl = ResolveApiBaseUrl(arguments, swaggerSource);
        var catalog = await LoadCatalogAsync(arguments, cancellationToken).ConfigureAwait(false);

        var operation = ResolveLoginOperation(arguments, catalog);
        var username = Require(arguments, "username");
        var password = Require(arguments, "password");

        var actionArguments = BuildActionArguments(operation, arguments);
        var requestBodyJson = arguments.TryGetValue("requestBodyJson", out var explicitBody) && !string.IsNullOrWhiteSpace(explicitBody)
            ? explicitBody
            : JsonSerializer.Serialize(new { username, password });

        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true
        };
        var client = new HttpClient(handler);

        var executor = new HttpApiExecutor(client, apiBaseUrl, catalog.ServerBasePath);
        var action = new PlannedAction(operation.OperationId, actionArguments, requestBodyJson);
        var result = await executor.ExecuteAsync(operation, action, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            client.Dispose();
            handler.Dispose();
            throw new InvalidOperationException($"Login failed ({(int)result.StatusCode.GetValueOrDefault()}). {result.ResponseBody}");
        }

        var session = new ApiSession(client, handler, cookieContainer);
        var key = BuildSessionKey(userId, apiBaseUrl);
        _apiSessions.AddOrUpdate(
            key,
            _ => session,
            (_, existing) =>
            {
                existing.Dispose();
                return session;
            });

        var cookieCount = cookieContainer.GetCookies(apiBaseUrl).Count;

        return CreateToolResult(
            $"Logged in as '{userId}' using operation '{operation.OperationId}'.",
            new
            {
                userId,
                operationId = operation.OperationId,
                statusCode = result.StatusCode.HasValue ? (int?)result.StatusCode.Value : null,
                cookieCount,
                responseBody = result.ResponseBody
            });
    }

    private async Task<object> RunUseCaseAsync(IReadOnlyDictionary<string, string?> arguments, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId(arguments);
        var goal = Require(arguments, "goal");
        var swaggerSource = ResolveSwaggerUrl(arguments) ?? ResolveSwaggerFile(arguments);
        var apiBaseUrl = ResolveApiBaseUrl(arguments, swaggerSource);
        var catalog = await LoadCatalogAsync(arguments, cancellationToken).ConfigureAwait(false);

        string apiAuthMode;
        HttpClient? ephemeralHttpClient = null;
        HttpApiExecutor executor;

        if (TryGetSession(userId, apiBaseUrl, out var session) && session is not null)
        {
            executor = new HttpApiExecutor(session.Client, apiBaseUrl, catalog.ServerBasePath);
            apiAuthMode = "session_cookie";
        }
        else
        {
            ephemeralHttpClient = new HttpClient();
            executor = new HttpApiExecutor(ephemeralHttpClient, apiBaseUrl, catalog.ServerBasePath);
            apiAuthMode = "none";
        }

        try
        {
            IUseCasePlanner planner;
            string planningMode;

            if (arguments.TryGetValue("planJson", out var planJson) && !string.IsNullOrWhiteSpace(planJson))
            {
                var parsedPlan = new UseCasePlanJsonParser().Parse(planJson, catalog);
                planner = new StaticUseCasePlanner(parsedPlan);
                planningMode = "client_hosted";
            }
            else
            {
                try
                {
                    var settings = await _userModelSettingsProvider.GetAsync(userId, cancellationToken).ConfigureAwait(false);
                    var client = _textGenerationClientFactory.Create(settings);
                    planner = new ValidatingUseCasePlanner(client);
                    planningMode = "user_key";
                }
                catch (InvalidOperationException)
                {
                    planner = new DeterministicFallbackPlanner();
                    planningMode = "fallback_no_license";
                }
                catch (NotSupportedException)
                {
                    planner = new DeterministicFallbackPlanner();
                    planningMode = "fallback_no_license";
                }
            }

            var orchestrator = new ApiAgentOrchestrator(_swaggerDocumentLoader, planner, executor);
            var result = await orchestrator.ExecuteAsync(catalog, new UseCaseRequest(goal), cancellationToken).ConfigureAwait(false);

            // Spara lyckade use_cases automatiskt
            if (result.Results.All(r => r.Succeeded))
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "logs", "successful_usecases");
                Directory.CreateDirectory(dir);
                var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{SanitizeFileName(result.Plan.Name)}.json";
                var filePath = Path.Combine(dir, fileName);
                var prettyJsonOptions = new System.Text.Json.JsonSerializerOptions(JsonOptions)
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                File.WriteAllText(filePath, System.Text.Json.JsonSerializer.Serialize(result, prettyJsonOptions));
            }
            // Möjlighet att spara misslyckade som change request
            else if (arguments.TryGetValue("saveAsChangeRequest", out var saveChangeReq) && saveChangeReq == "true")
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "logs", "change_requests");
                Directory.CreateDirectory(dir);
                var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{SanitizeFileName(result.Plan.Name)}.json";
                var filePath = Path.Combine(dir, fileName);
                var changeRequest = new {
                    Plan = result.Plan,
                    Results = result.Results,
                    Comment = arguments.TryGetValue("changeRequestComment", out var comment) ? comment : null
                };
                var prettyJsonOptions = new System.Text.Json.JsonSerializerOptions(JsonOptions)
                {
                    WriteIndented = true
                };
                File.WriteAllText(filePath, System.Text.Json.JsonSerializer.Serialize(changeRequest, prettyJsonOptions));
            }

            return CreateToolResult(
                $"Executed plan '{result.Plan.Name}' with {result.Results.Count} result(s).",
                new
                {
                    planningMode,
                    apiAuthMode,
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
        finally
        {
            ephemeralHttpClient?.Dispose();
        }

        // Hjälpmetod för filnamn
        static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }

    private static SwaggerOperation ResolveLoginOperation(IReadOnlyDictionary<string, string?> arguments, SwaggerDocumentCatalog catalog)
    {
        if (arguments.TryGetValue("loginOperationId", out var explicitOperationId) && !string.IsNullOrWhiteSpace(explicitOperationId))
        {
            return catalog.GetRequiredOperation(explicitOperationId);
        }

        if (arguments.TryGetValue("loginPath", out var explicitLoginPath) && !string.IsNullOrWhiteSpace(explicitLoginPath))
        {
            var normalizedPath = NormalizePath(explicitLoginPath);
            var byPath = catalog.Operations.FirstOrDefault(operation =>
                operation.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                && NormalizePath(operation.Path).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

            if (byPath is not null)
            {
                return byPath;
            }
        }

        var inferred = catalog.Operations.FirstOrDefault(operation =>
            operation.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && (Contains(operation.OperationId, "login")
                || Contains(operation.Summary, "login")
                || Contains(operation.Path, "login")
                || Contains(operation.OperationId, "signin")
                || Contains(operation.Summary, "signin")
                || Contains(operation.Path, "signin")
                || Contains(operation.OperationId, "auth")
                || Contains(operation.Summary, "auth")
                || Contains(operation.Path, "auth")
                || Contains(operation.OperationId, "session")
                || Contains(operation.Summary, "session")
                || Contains(operation.Path, "session")));

        if (inferred is not null)
        {
            return inferred;
        }

        throw new InvalidOperationException("Could not infer login operation. Provide 'loginOperationId' or 'loginPath'.");
    }

    private static IReadOnlyDictionary<string, string?> BuildActionArguments(SwaggerOperation operation, IReadOnlyDictionary<string, string?> arguments)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in operation.Parameters)
        {
            if (arguments.TryGetValue(parameter.Name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                result[parameter.Name] = value;
            }

            if (parameter.Location.Equals("path", StringComparison.OrdinalIgnoreCase)
                && parameter.Required
                && (!result.TryGetValue(parameter.Name, out var requiredValue) || string.IsNullOrWhiteSpace(requiredValue)))
            {
                throw new InvalidOperationException($"Missing required login path parameter '{parameter.Name}'.");
            }
        }

        return result;
    }

    private static string NormalizePath(string path)
    {
        var value = path.Split('?', 2)[0].Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        return value.Length > 1 ? value.TrimEnd('/') : value;
    }

    private bool TryGetSession(string userId, Uri apiBaseUrl, out ApiSession? session)
    {
        return _apiSessions.TryGetValue(BuildSessionKey(userId, apiBaseUrl), out session);
    }

    private static string BuildSessionKey(string userId, Uri apiBaseUrl)
    {
        return $"{userId.ToLowerInvariant()}|{apiBaseUrl.GetLeftPart(UriPartial.Authority).ToLowerInvariant()}";
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

        throw new InvalidOperationException("Specify apiBaseUrl either on the tool call or when launching the server (use --api-base-url when starting MCP, especially with --swagger-file).");
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

        // MVP default: single-session server mode when client does not provide userId.
        return "default-user";
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

    internal sealed class ApiSession : IDisposable
    {
        public ApiSession(HttpClient client, HttpClientHandler handler, CookieContainer cookies)
        {
            Client = client;
            Handler = handler;
            Cookies = cookies;
        }

        public HttpClient Client { get; }
        public HttpClientHandler Handler { get; }
        public CookieContainer Cookies { get; }

        public void Dispose()
        {
            Client.Dispose();
            Handler.Dispose();
        }
    }

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
