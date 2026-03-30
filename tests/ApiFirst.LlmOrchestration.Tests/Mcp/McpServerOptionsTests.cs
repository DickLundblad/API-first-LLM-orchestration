using ApiFirst.LlmOrchestration.McpServer;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests.Mcp;

public sealed class McpServerOptionsTests
{
    [Test]
    public void Parse_supports_default_source_and_user_options()
    {
        var options = McpServerOptions.Parse(new[]
        {
            "--user-id", "alice",
            "--swagger-url", "https://example.test/swagger.json",
            "--api-base-url", "https://example.test"
        });

        Assert.That(options.Help, Is.False);
        Assert.That(options.DefaultUserId, Is.EqualTo("alice"));
        Assert.That(options.DefaultSwaggerUrl, Is.EqualTo("https://example.test/swagger.json"));
        Assert.That(options.DefaultSwaggerFile, Is.Null);
        Assert.That(options.DefaultApiBaseUrl, Is.EqualTo("https://example.test"));
    }

    [Test]
    public void Parse_supports_http_prefix()
    {
        var options = McpServerOptions.Parse(new[]
        {
            "--http-prefix", "http://localhost:5055/"
        });

        Assert.That(options.HttpPrefix, Is.EqualTo("http://localhost:5055/"));
    }
    [Test]
    public void Parse_rejects_both_default_swagger_sources()
    {
        var ex = Assert.Throws<McpUsageException>(() => McpServerOptions.Parse(new[]
        {
            "--swagger-url", "https://example.test/swagger.json",
            "--swagger-file", ".\\swagger.json"
        }));

        Assert.That(ex!.Message, Does.Contain("only one"));
    }
}

