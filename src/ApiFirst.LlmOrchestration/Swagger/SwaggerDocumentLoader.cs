using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;
using System.Text.Json;

namespace ApiFirst.LlmOrchestration.Swagger;

public sealed class SwaggerDocumentLoader : ISwaggerDocumentLoader
{
    public async Task<SwaggerDocumentCatalog> LoadFromFileAsync(string swaggerJsonPath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(swaggerJsonPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Parse(document.RootElement);
    }

    public async Task<SwaggerDocumentCatalog> LoadFromUrlAsync(string swaggerJsonUrl, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync(swaggerJsonUrl, cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);
        return Parse(document.RootElement);
    }

    private static SwaggerDocumentCatalog Parse(JsonElement root)
    {
        if (!root.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("The swagger document does not contain a valid 'paths' section.");
        }

        var title = TryGetInfoString(root, "title");
        var version = TryGetInfoString(root, "version");
        var description = TryGetInfoString(root, "description");
        var serverBasePath = ParseServerBasePath(root);

        var operations = new List<SwaggerOperation>();

        foreach (var pathProperty in paths.EnumerateObject())
        {
            foreach (var methodProperty in pathProperty.Value.EnumerateObject())
            {
                if (!IsHttpMethod(methodProperty.Name))
                {
                    continue;
                }

                var operationElement = methodProperty.Value;
                var operationId = GetOptionalString(operationElement, "operationId") ?? CreateOperationId(methodProperty.Name, pathProperty.Name);
                var summary = operationElement.TryGetProperty("summary", out var summaryElement) ? summaryElement.GetString() : null;
                var tags = ParseStringArray(operationElement, "tags");
                var hasRequestBody = operationElement.TryGetProperty("requestBody", out _);
                var securityRequirements = ParseSecurityRequirements(operationElement);
                var responseStatusCodes = ParseResponseStatusCodes(operationElement);

                var parameters = new List<SwaggerParameter>();
                if (operationElement.TryGetProperty("parameters", out var parametersElement) && parametersElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var parameterElement in parametersElement.EnumerateArray())
                    {
                        parameters.Add(ParseParameter(parameterElement));
                    }
                }

                operations.Add(new SwaggerOperation(
                    pathProperty.Name,
                    methodProperty.Name.ToUpperInvariant(),
                    operationId,
                    summary,
                    parameters,
                    tags,
                    hasRequestBody,
                    securityRequirements,
                    responseStatusCodes));
            }
        }

        return new SwaggerDocumentCatalog(operations, serverBasePath, title, version, description);
    }

    private static SwaggerParameter ParseParameter(JsonElement parameterElement)
    {
        var name = GetRequiredString(parameterElement, "name", "parameter");
        var location = GetRequiredString(parameterElement, "in", name);
        var required = parameterElement.TryGetProperty("required", out var requiredElement) && requiredElement.ValueKind == JsonValueKind.True;

        string? schemaType = null;
        if (parameterElement.TryGetProperty("schema", out var schemaElement) && schemaElement.ValueKind == JsonValueKind.Object)
        {
            schemaType = schemaElement.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
        }
        else if (parameterElement.TryGetProperty("type", out var typeValue))
        {
            schemaType = typeValue.GetString();
        }

        return new SwaggerParameter(name, location, required, schemaType);
    }

    private static bool IsHttpMethod(string name)
    {
        return name.Equals("get", StringComparison.OrdinalIgnoreCase)
            || name.Equals("put", StringComparison.OrdinalIgnoreCase)
            || name.Equals("post", StringComparison.OrdinalIgnoreCase)
            || name.Equals("delete", StringComparison.OrdinalIgnoreCase)
            || name.Equals("patch", StringComparison.OrdinalIgnoreCase)
            || name.Equals("head", StringComparison.OrdinalIgnoreCase)
            || name.Equals("options", StringComparison.OrdinalIgnoreCase)
            || name.Equals("trace", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateOperationId(string method, string path)
    {
        var tokens = path
            .Split(new[] { '/', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(token => token.Trim('{', '}').Split(new[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries))
            .SelectMany(token => token.Split(new[] { '/', '-', '_', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            .Where(token => token.Length > 0)
            .Select(token => char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant());

        return char.ToUpperInvariant(method[0]) + method[1..].ToLowerInvariant() + string.Concat(tokens);
    }

    private static string? ParseServerBasePath(JsonElement root)
    {
        if (!root.TryGetProperty("servers", out var servers) || servers.ValueKind != JsonValueKind.Array || servers.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var firstServer = servers[0];
        if (!firstServer.TryGetProperty("url", out var urlElement))
        {
            return string.Empty;
        }

        return urlElement.GetString();
    }

    private static string? TryGetInfoString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty("info", out var infoElement) || infoElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return infoElement.TryGetProperty(propertyName, out var valueElement) ? valueElement.GetString() : null;
    }

    private static IReadOnlyList<string> ParseStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var arrayElement) || arrayElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var values = new List<string>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static IReadOnlyList<string> ParseSecurityRequirements(JsonElement operationElement)
    {
        if (!operationElement.TryGetProperty("security", out var securityElement) || securityElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var requirements = new List<string>();
        foreach (var entry in securityElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var securityName in entry.EnumerateObject())
            {
                requirements.Add(securityName.Name);
            }
        }

        return requirements;
    }

    private static IReadOnlyList<int> ParseResponseStatusCodes(JsonElement operationElement)
    {
        if (!operationElement.TryGetProperty("responses", out var responsesElement) || responsesElement.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<int>();
        }

        var statusCodes = new List<int>();
        foreach (var response in responsesElement.EnumerateObject())
        {
            if (int.TryParse(response.Name, out var code))
            {
                statusCodes.Add(code);
            }
        }

        return statusCodes;
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var valueElement))
        {
            return null;
        }

        var value = valueElement.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string GetRequiredString(JsonElement element, string propertyName, params string[] contextParts)
    {
        var context = string.Join(" ", contextParts);

        if (!element.TryGetProperty(propertyName, out var valueElement))
        {
            throw new InvalidOperationException($"The swagger document is missing '{propertyName}' for '{context}'.");
        }

        var value = valueElement.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"The swagger document contains an empty '{propertyName}' for '{context}'.");
        }

        return value;
    }
}
