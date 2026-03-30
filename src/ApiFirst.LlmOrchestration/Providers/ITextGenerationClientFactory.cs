using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Configuration;

namespace ApiFirst.LlmOrchestration.Providers;

public interface ITextGenerationClientFactory
{
    ITextGenerationClient Create(UserModelSettings settings);
}
