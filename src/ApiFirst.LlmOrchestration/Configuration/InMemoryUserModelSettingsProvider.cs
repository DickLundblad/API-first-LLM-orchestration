namespace ApiFirst.LlmOrchestration.Configuration;

public sealed class InMemoryUserModelSettingsProvider : IUserModelSettingsProvider
{
    private readonly IReadOnlyDictionary<string, UserModelSettings> _settingsByUserId;

    public InMemoryUserModelSettingsProvider(IEnumerable<UserModelSettings> settings)
    {
        _settingsByUserId = settings.ToDictionary(setting => setting.UserId, StringComparer.OrdinalIgnoreCase);
    }

    public Task<UserModelSettings> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!_settingsByUserId.TryGetValue(userId, out var settings))
        {
            throw new KeyNotFoundException($"No model settings were configured for user '{userId}'.");
        }

        return Task.FromResult(settings);
    }
}
