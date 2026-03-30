namespace ApiFirst.LlmOrchestration.Cli;

public sealed record CliOptions(
    bool Help,
    bool ListOperations,
    string? UserId,
    string? SwaggerUrl,
    string? SwaggerFile,
    string? Goal,
    string ApiBaseUrl)
{
    public static CliOptions Parse(string[] args)
    {
        if (args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase) || arg.Equals("/?", StringComparison.OrdinalIgnoreCase)))
        {
            return new CliOptions(true, false, null, null, null, null, string.Empty);
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var listOperations = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--list-operations", StringComparison.OrdinalIgnoreCase))
            {
                listOperations = true;
                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                throw new CliUsageException($"Unexpected argument '{arg}'. Use --user-id, --swagger-url or --swagger-file, --goal, optional --list-operations, and optional --api-base-url.");
            }

            var key = arg[2..];
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new CliUsageException($"Missing value for '{arg}'.");
            }

            values[key] = args[++i];
        }

        var userId = Require(values, "user-id");
        var goal = listOperations ? values.TryGetValue("goal", out var goalValue) ? goalValue : null : Require(values, "goal");
        var swaggerUrl = values.TryGetValue("swagger-url", out var swaggerUrlValue) ? swaggerUrlValue : null;
        var swaggerFile = values.TryGetValue("swagger-file", out var swaggerFileValue) ? swaggerFileValue : null;

        if (string.IsNullOrWhiteSpace(swaggerUrl) == string.IsNullOrWhiteSpace(swaggerFile))
        {
            throw new CliUsageException("Specify exactly one of '--swagger-url' or '--swagger-file'.");
        }

        if (!listOperations && string.IsNullOrWhiteSpace(goal))
        {
            throw new CliUsageException("Missing required option '--goal'.");
        }

        var apiBaseUrl = values.TryGetValue("api-base-url", out var apiBaseValue) && !string.IsNullOrWhiteSpace(apiBaseValue)
            ? apiBaseValue
            : swaggerUrl is not null
                ? InferApiBaseUrl(swaggerUrl)
                : string.Empty;

        return new CliOptions(false, listOperations, userId, swaggerUrl, swaggerFile, goal, apiBaseUrl);
    }

    private static string Require(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new CliUsageException($"Missing required option '--{key}'.");
    }

    private static string InferApiBaseUrl(string swaggerUrl)
    {
        var uri = new Uri(swaggerUrl, UriKind.Absolute);
        return uri.GetLeftPart(UriPartial.Authority);
    }
}
