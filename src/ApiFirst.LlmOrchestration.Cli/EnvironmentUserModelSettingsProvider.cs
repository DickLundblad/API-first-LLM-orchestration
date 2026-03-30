using ApiFirst.LlmOrchestration.Configuration;

namespace ApiFirst.LlmOrchestration.Cli;

public sealed class EnvironmentUserModelSettingsProvider : IUserModelSettingsProvider
{
    public Task<UserModelSettings> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var prefix = $"API_ORCH_{Sanitize(userId).ToUpperInvariant()}_";

        var provider = Require(prefix + "PROVIDER");
        var model = Require(prefix + "MODEL");
        var apiKey = Require(prefix + "API_KEY");
        var endpoint = TryParseUri(Environment.GetEnvironmentVariable(prefix + "ENDPOINT"));
        var reasoningEffort = Environment.GetEnvironmentVariable(prefix + "REASONING_EFFORT");
        var systemInstructions = Environment.GetEnvironmentVariable(prefix + "SYSTEM_INSTRUCTIONS");

        return Task.FromResult(new UserModelSettings(userId, provider, model, apiKey, endpoint, reasoningEffort, systemInstructions));
    }

    private static string Require(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing environment variable '{name}'.");
        }

        return value;
    }

    private static Uri? TryParseUri(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static string Sanitize(string userId)
    {
        return new string(userId.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
    }
}
