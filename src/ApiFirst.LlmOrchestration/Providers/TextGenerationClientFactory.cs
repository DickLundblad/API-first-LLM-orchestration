using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Configuration;

namespace ApiFirst.LlmOrchestration.Providers;

public sealed class TextGenerationClientFactory : ITextGenerationClientFactory
{
    private readonly HttpClient _httpClient;

    public TextGenerationClientFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public ITextGenerationClient Create(UserModelSettings settings)
    {
        if (settings.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
        {
            return new OpenAITextGenerationClient(_httpClient, settings);
        }

        throw new NotSupportedException($"Unsupported model provider '{settings.Provider}'.");
    }
}
