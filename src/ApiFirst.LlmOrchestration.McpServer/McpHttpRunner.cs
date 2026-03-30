using System.Net.Sockets;
using System.Text;

namespace ApiFirst.LlmOrchestration.McpServer;

public static class McpHttpRunner
{
    public static async Task RunAsync(McpServer server, string prefix, CancellationToken cancellationToken)
    {
        var uri = new Uri(prefix.EndsWith('/') ? prefix : prefix + '/');
        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            throw new McpUsageException("--http-prefix must use http:// and point at a local port.");
        }

        var listener = new TcpListener(System.Net.IPAddress.Loopback, uri.Port);
        listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = HandleClientAsync(server, client, cancellationToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task HandleClientAsync(McpServer server, TcpClient client, CancellationToken cancellationToken)
    {
        using var clientHandle = client;
        var stream = client.GetStream();

        try
        {
            var request = await ReadRequestAsync(stream, cancellationToken).ConfigureAwait(false);
            if (request is null)
            {
                return;
            }

            if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) && request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(stream, 200, "ok", "text/plain; charset=utf-8", cancellationToken).ConfigureAwait(false);
                return;
            }

            if (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) && request.Path.Equals("/mcp", StringComparison.OrdinalIgnoreCase))
            {
                using var output = new StringWriter();
                await server.RunAsync(new StringReader(request.Body), output, TextWriter.Null, cancellationToken).ConfigureAwait(false);
                await WriteResponseAsync(stream, 200, output.ToString(), "application/json; charset=utf-8", cancellationToken).ConfigureAwait(false);
                return;
            }

            await WriteResponseAsync(stream, 404, "Not found", "text/plain; charset=utf-8", cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await WriteResponseAsync(stream, 500, ex.Message, "text/plain; charset=utf-8", cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<HttpRequest?> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var headerBytes = new List<byte>();
        var buffer = new byte[1];
        var matchState = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return null;
            }

            headerBytes.Add(buffer[0]);
            matchState = buffer[0] switch
            {
                (byte)'\r' when matchState is 0 or 2 => matchState + 1,
                (byte)'\n' when matchState is 1 or 3 => matchState + 1,
                _ => 0
            };

            if (matchState == 4)
            {
                break;
            }
        }

        var headerText = Encoding.ASCII.GetString(headerBytes.ToArray());
        var headerLines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (headerLines.Length == 0)
        {
            return null;
        }

        var requestLineParts = headerLines[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (requestLineParts.Length < 2)
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < headerLines.Length; i++)
        {
            var colonIndex = headerLines[i].IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var name = headerLines[i][..colonIndex].Trim();
            var value = headerLines[i][(colonIndex + 1)..].Trim();
            headers[name] = value;
        }

        var contentLength = 0;
        if (headers.TryGetValue("Content-Length", out var contentLengthValue) && int.TryParse(contentLengthValue, out var parsedLength) && parsedLength > 0)
        {
            contentLength = parsedLength;
        }

        var bodyBytes = new byte[contentLength];
        var offset = 0;
        while (offset < contentLength)
        {
            var read = await stream.ReadAsync(bodyBytes.AsMemory(offset, contentLength - offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        return new HttpRequest(
            requestLineParts[0],
            requestLineParts[1],
            Encoding.UTF8.GetString(bodyBytes, 0, offset));
    }

    private static async Task WriteResponseAsync(NetworkStream stream, int statusCode, string body, string contentType, CancellationToken cancellationToken)
    {
        var reasonPhrase = statusCode switch
        {
            200 => "OK",
            404 => "Not Found",
            405 => "Method Not Allowed",
            500 => "Internal Server Error",
            _ => "OK"
        };

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var headers = $"HTTP/1.1 {statusCode} {reasonPhrase}\r\n" +
                      $"Content-Type: {contentType}\r\n" +
                      $"Content-Length: {bodyBytes.Length}\r\n" +
                      "Connection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(headers);
        await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bodyBytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record HttpRequest(string Method, string Path, string Body);
}

