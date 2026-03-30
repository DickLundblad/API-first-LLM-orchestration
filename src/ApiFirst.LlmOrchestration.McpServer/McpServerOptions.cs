namespace ApiFirst.LlmOrchestration.McpServer;

public sealed record McpServerOptions(
    bool Help,
    string? DefaultUserId,
    string? DefaultSwaggerUrl,
    string? DefaultSwaggerFile,
    string? DefaultApiBaseUrl,
    string? HttpPrefix)
{
    public static McpServerOptions Parse(string[] args)
    {
        string? defaultUserId = null;
        string? defaultSwaggerUrl = null;
        string? defaultSwaggerFile = null;
        string? defaultApiBaseUrl = null;
        string? httpPrefix = null;
        var help = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help":
                case "-h":
                case "/?":
                    help = true;
                    break;
                case "--user-id":
                    defaultUserId = RequireValue(args, ref i, arg);
                    break;
                case "--swagger-url":
                    defaultSwaggerUrl = RequireValue(args, ref i, arg);
                    break;
                case "--swagger-file":
                    defaultSwaggerFile = RequireValue(args, ref i, arg);
                    break;
                case "--api-base-url":
                    defaultApiBaseUrl = RequireValue(args, ref i, arg);
                    break;
                case "--http-prefix":
                    httpPrefix = RequireValue(args, ref i, arg);
                    break;
                default:
                    throw new McpUsageException($"Unknown argument '{arg}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(defaultSwaggerUrl) && !string.IsNullOrWhiteSpace(defaultSwaggerFile))
        {
            throw new McpUsageException("Specify only one of '--swagger-url' or '--swagger-file'.");
        }

        if (!string.IsNullOrWhiteSpace(defaultApiBaseUrl))
        {
            _ = new Uri(defaultApiBaseUrl, UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(httpPrefix))
        {
            _ = new Uri(httpPrefix, UriKind.Absolute);
        }

        return new McpServerOptions(help, defaultUserId, defaultSwaggerUrl, defaultSwaggerFile, defaultApiBaseUrl, httpPrefix);
    }

    private static string RequireValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new McpUsageException($"Missing value for '{name}'.");
        }

        index++;
        return args[index];
    }
}
