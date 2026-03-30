namespace ApiFirst.LlmOrchestration.Configuration;

public interface IUserModelSettingsProvider
{
    Task<UserModelSettings> GetAsync(string userId, CancellationToken cancellationToken = default);
}
