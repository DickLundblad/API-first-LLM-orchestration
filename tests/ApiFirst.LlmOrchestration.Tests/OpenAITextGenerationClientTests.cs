using ApiFirst.LlmOrchestration.Configuration;
using ApiFirst.LlmOrchestration.Providers;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests;

public sealed class OpenAITextGenerationClientTests
{
    [Test]
    public async Task GenerateTextAsync_posts_to_the_responses_endpoint_with_the_user_key()
    {
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler);
        var settings = new UserModelSettings(
            "alice",
            "openai",
            "gpt-5.4-mini",
            "user-secret-key",
            new Uri("https://api.openai.com/v1/responses"),
            "low",
            "Be concise.");

        var client = new OpenAITextGenerationClient(httpClient, settings);

        var text = await client.GenerateTextAsync("Plan a customer lookup");

        Assert.That(text, Is.EqualTo("""{"name":"ok"}"""));
        Assert.That(handler.CapturedRequest, Is.Not.Null);
        Assert.That(handler.CapturedRequest!.RequestUri!.AbsoluteUri, Is.EqualTo("https://api.openai.com/v1/responses"));
        Assert.That(handler.CapturedRequest.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(handler.CapturedRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(handler.CapturedRequest.Headers.Authorization!.Parameter, Is.EqualTo("user-secret-key"));
        Assert.That(handler.CapturedBody, Does.Contain("gpt-5.4-mini"));
        Assert.That(handler.CapturedBody, Does.Contain("Plan a customer lookup"));
        Assert.That(handler.CapturedBody, Does.Contain("Be concise."));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string CapturedBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            CapturedBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "output_text": "{\"name\":\"ok\"}"
                    }
                    """)
            };
        }
    }
}

