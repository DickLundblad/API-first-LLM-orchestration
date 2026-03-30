using ApiFirst.LlmOrchestration.Cli;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests.Cli;

public sealed class EnvironmentUserModelSettingsProviderTests
{
    [Test]
    public async Task GetAsync_reads_user_specific_environment_variables()
    {
        var previousProvider = Environment.GetEnvironmentVariable("API_ORCH_ALICE_PROVIDER");
        var previousModel = Environment.GetEnvironmentVariable("API_ORCH_ALICE_MODEL");
        var previousApiKey = Environment.GetEnvironmentVariable("API_ORCH_ALICE_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("API_ORCH_ALICE_PROVIDER", "openai");
            Environment.SetEnvironmentVariable("API_ORCH_ALICE_MODEL", "gpt-5.4-mini");
            Environment.SetEnvironmentVariable("API_ORCH_ALICE_API_KEY", "alice-key");

            var provider = new EnvironmentUserModelSettingsProvider();
            var settings = await provider.GetAsync("alice");

            Assert.That(settings.Provider, Is.EqualTo("openai"));
            Assert.That(settings.Model, Is.EqualTo("gpt-5.4-mini"));
            Assert.That(settings.ApiKey, Is.EqualTo("alice-key"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("API_ORCH_ALICE_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("API_ORCH_ALICE_MODEL", previousModel);
            Environment.SetEnvironmentVariable("API_ORCH_ALICE_API_KEY", previousApiKey);
        }
    }
}
