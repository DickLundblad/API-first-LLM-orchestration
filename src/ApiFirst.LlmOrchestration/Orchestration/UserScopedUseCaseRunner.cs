using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Configuration;
using ApiFirst.LlmOrchestration.Models;
using ApiFirst.LlmOrchestration.Planning;
using ApiFirst.LlmOrchestration.Providers;

namespace ApiFirst.LlmOrchestration.Orchestration;

public sealed class UserScopedUseCaseRunner
{
    private readonly IUserModelSettingsProvider _userModelSettingsProvider;
    private readonly ITextGenerationClientFactory _textGenerationClientFactory;
    private readonly ISwaggerDocumentLoader _swaggerDocumentLoader;
    private readonly IApiExecutor _apiExecutor;

    public UserScopedUseCaseRunner(
        IUserModelSettingsProvider userModelSettingsProvider,
        ITextGenerationClientFactory textGenerationClientFactory,
        ISwaggerDocumentLoader swaggerDocumentLoader,
        IApiExecutor apiExecutor)
    {
        _userModelSettingsProvider = userModelSettingsProvider;
        _textGenerationClientFactory = textGenerationClientFactory;
        _swaggerDocumentLoader = swaggerDocumentLoader;
        _apiExecutor = apiExecutor;
    }

    public Task<UseCaseExecutionResult> RunFromFileAsync(
        string userId,
        string swaggerJsonPath,
        UseCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(userId, request, cancellationToken, loader => loader.LoadFromFileAsync(swaggerJsonPath, cancellationToken));
    }

    public Task<UseCaseExecutionResult> RunFromUrlAsync(
        string userId,
        string swaggerJsonUrl,
        UseCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(userId, request, cancellationToken, loader => loader.LoadFromUrlAsync(swaggerJsonUrl, cancellationToken));
    }

    private async Task<UseCaseExecutionResult> RunAsync(
        string userId,
        UseCaseRequest request,
        CancellationToken cancellationToken,
        Func<ISwaggerDocumentLoader, Task<SwaggerDocumentCatalog>> loadCatalog)
    {
        var userSettings = await _userModelSettingsProvider.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        var client = _textGenerationClientFactory.Create(userSettings);
        var planner = new ValidatingUseCasePlanner(client);
        var catalog = await loadCatalog(_swaggerDocumentLoader).ConfigureAwait(false);
        var orchestrator = new ApiAgentOrchestrator(_swaggerDocumentLoader, planner, _apiExecutor);

        return await orchestrator.ExecuteAsync(catalog, request, cancellationToken).ConfigureAwait(false);
    }
}
