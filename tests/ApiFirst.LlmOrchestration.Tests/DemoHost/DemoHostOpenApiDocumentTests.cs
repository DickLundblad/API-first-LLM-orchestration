using NUnit.Framework;
using ApiFirst.LlmOrchestration.DemoHost;

namespace ApiFirst.LlmOrchestration.Tests.DemoHost;

public sealed class DemoHostOpenApiDocumentTests
{
    [Test]
    public void Build_embeds_the_base_url_and_demo_operations()
    {
        var json = DemoHostOpenApiDocument.Build("https://example.ngrok-free.app");

        Assert.That(json, Does.Contain("https://example.ngrok-free.app"));
        Assert.That(json, Does.Contain("/operations"));
        Assert.That(json, Does.Contain("RunUseCase"));
    }
}
