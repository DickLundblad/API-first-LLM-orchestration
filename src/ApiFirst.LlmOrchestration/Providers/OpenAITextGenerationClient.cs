using System.Text;
using System.Text.Json;
using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Configuration;

namespace ApiFirst.LlmOrchestration.Providers;

public sealed class OpenAITextGenerationClient : ITextGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly UserModelSettings _settings;
    private readonly Uri _responsesEndpoint;

    public OpenAITextGenerationClient(HttpClient httpClient, UserModelSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
        _responsesEndpoint = settings.Endpoint ?? new Uri("https://api.openai.com/v1/responses", UriKind.Absolute);
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = _settings.Model,
            ["input"] = prompt
        };

        if (!string.IsNullOrWhiteSpace(_settings.ReasoningEffort))
        {
            requestBody["reasoning"] = new Dictionary<string, string>
            {
                ["effort"] = _settings.ReasoningEffort
            };
        }

        if (!string.IsNullOrWhiteSpace(_settings.SystemInstructions))
        {
            requestBody["instructions"] = _settings.SystemInstructions;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _responsesEndpoint)
        {
            Headers =
            {
                Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey)
            },
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI request failed with {(int)response.StatusCode}: {responseJson}");
        }

        return ExtractOutputText(responseJson);
    }

    private static string ExtractOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputTextElement))
        {
            var outputText = outputTextElement.GetString();
            if (!string.IsNullOrWhiteSpace(outputText))
            {
                return outputText;
            }
        }

        if (!root.TryGetProperty("output", out var outputElement) || outputElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("The OpenAI response did not contain output text.");
        }

        var builder = new StringBuilder();
        foreach (var item in outputElement.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var contentElement) || contentElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentPart in contentElement.EnumerateArray())
            {
                if (!contentPart.TryGetProperty("type", out var typeElement))
                {
                    continue;
                }

                var type = typeElement.GetString();
                if (!string.Equals(type, "output_text", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (contentPart.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        builder.Append(text);
                    }
                }
            }
        }

        if (builder.Length == 0)
        {
            throw new InvalidOperationException("The OpenAI response did not contain any output_text content.");
        }

        return builder.ToString();
    }
}
