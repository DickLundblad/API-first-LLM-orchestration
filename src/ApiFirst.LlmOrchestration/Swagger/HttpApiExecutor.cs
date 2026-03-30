using System.Net.Http.Headers;
using System.Text;
using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Swagger;

public sealed class HttpApiExecutor : IApiExecutor
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseAddress;
    private readonly string _serverBasePath;

    public HttpApiExecutor(HttpClient httpClient, Uri baseAddress, string? serverBasePath = null, string? bearerToken = null)
    {
        _httpClient = httpClient;
        _baseAddress = NormalizeBaseAddress(baseAddress);
        _serverBasePath = NormalizeServerBasePath(serverBasePath);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    public async Task<PlannedActionResult> ExecuteAsync(
        SwaggerOperation operation,
        PlannedAction action,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri(operation, action.Arguments);
        using var request = new HttpRequestMessage(new HttpMethod(operation.Method), requestUri);

        if (!string.IsNullOrWhiteSpace(action.RequestBodyJson))
        {
            request.Content = new StringContent(action.RequestBodyJson, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new PlannedActionResult(
            action.OperationId,
            response.IsSuccessStatusCode,
            response.StatusCode,
            responseBody);
    }

    private Uri BuildRequestUri(SwaggerOperation operation, IReadOnlyDictionary<string, string?> arguments)
    {
        var path = operation.Path;
        var queryParts = new List<string>();

        foreach (var parameter in operation.Parameters)
        {
            arguments.TryGetValue(parameter.Name, out var value);

            if (parameter.Location.Equals("path", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException($"Missing path parameter '{parameter.Name}' for operation '{operation.OperationId}'.");
                }

                path = path.Replace("{" + parameter.Name + "}", Uri.EscapeDataString(value), StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (parameter.Location.Equals("query", StringComparison.OrdinalIgnoreCase) && value is not null)
            {
                queryParts.Add(Uri.EscapeDataString(parameter.Name) + "=" + Uri.EscapeDataString(value));
            }
        }

        var combinedPath = CombinePaths(_serverBasePath, path);
        var requestUri = new Uri(_baseAddress, combinedPath.TrimStart('/'));
        var builder = new UriBuilder(requestUri)
        {
            Query = string.Join("&", queryParts)
        };

        return builder.Uri;
    }

    private static string CombinePaths(string serverBasePath, string operationPath)
    {
        if (string.IsNullOrWhiteSpace(serverBasePath))
        {
            return operationPath;
        }

        if (string.IsNullOrWhiteSpace(operationPath))
        {
            return serverBasePath;
        }

        return serverBasePath.TrimEnd('/') + "/" + operationPath.TrimStart('/');
    }

    private static Uri NormalizeBaseAddress(Uri baseAddress)
    {
        return baseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? baseAddress
            : new Uri(baseAddress.AbsoluteUri + "/", UriKind.Absolute);
    }

    private static string NormalizeServerBasePath(string? serverBasePath)
    {
        if (string.IsNullOrWhiteSpace(serverBasePath))
        {
            return string.Empty;
        }

        return serverBasePath.StartsWith('/') ? serverBasePath : "/" + serverBasePath;
    }
}
