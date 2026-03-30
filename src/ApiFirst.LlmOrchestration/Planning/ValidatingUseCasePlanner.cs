using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Planning;

public sealed class ValidatingUseCasePlanner : IUseCasePlanner
{
    private readonly ITextGenerationClient _textGenerationClient;
    private readonly UseCasePlanPromptBuilder _promptBuilder;
    private readonly UseCasePlanJsonParser _jsonParser;

    public ValidatingUseCasePlanner(
        ITextGenerationClient textGenerationClient,
        UseCasePlanPromptBuilder? promptBuilder = null,
        UseCasePlanJsonParser? jsonParser = null)
    {
        _textGenerationClient = textGenerationClient;
        _promptBuilder = promptBuilder ?? new UseCasePlanPromptBuilder();
        _jsonParser = jsonParser ?? new UseCasePlanJsonParser();
    }

    public async Task<UseCasePlan> CreatePlanAsync(
        UseCaseRequest request,
        SwaggerDocumentCatalog catalog,
        CancellationToken cancellationToken = default)
    {
        var prompt = _promptBuilder.Build(request, catalog);
        var response = await _textGenerationClient.GenerateTextAsync(prompt, cancellationToken).ConfigureAwait(false);

        return _jsonParser.Parse(response, catalog);
    }
}
