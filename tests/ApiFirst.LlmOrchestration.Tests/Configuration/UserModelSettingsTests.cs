using ApiFirst.LlmOrchestration.Configuration;
using ApiFirst.LlmOrchestration.Providers;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests.Configuration;

public sealed class UserModelSettingsTests
{
    [Test]
    public void InMemoryProvider_returns_the_settings_for_the_user()
    {
        var provider = new InMemoryUserModelSettingsProvider(new[]
        {
            new UserModelSettings("alice", "openai", "gpt-5.4-mini", "key-alice"),
            new UserModelSettings("bob", "openai", "gpt-5.4", "key-bob")
        });

        var settings = provider.GetAsync("alice").GetAwaiter().GetResult();

        Assert.That(settings.Model, Is.EqualTo("gpt-5.4-mini"));
        Assert.That(settings.ApiKey, Is.EqualTo("key-alice"));
    }

    [Test]
    public void TextGenerationClientFactory_creates_an_openai_client_for_openai_settings()
    {
        var factory = new TextGenerationClientFactory(new HttpClient(new StubHandler()));
        var client = factory.Create(new UserModelSettings("alice", "openai", "gpt-5.4-mini", "key-alice"));

        Assert.That(client, Is.InstanceOf<OpenAITextGenerationClient>());
    }

    [Test]
    public void TextGenerationClientFactory_rejects_unknown_providers()
    {
        var factory = new TextGenerationClientFactory(new HttpClient(new StubHandler()));

        Assert.Throws<NotSupportedException>(() =>
            factory.Create(new UserModelSettings("alice", "anthropic", "claude", "key-alice")));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
