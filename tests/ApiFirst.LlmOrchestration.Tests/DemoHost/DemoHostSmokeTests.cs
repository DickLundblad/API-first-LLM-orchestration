using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests.DemoHost;

public sealed class DemoHostSmokeTests
{
    [Test]
    public void Demo_run_request_shape_is_available()
    {
        var request = new global::DemoRunRequest("alice", "http://localhost/swagger.json", null, "List team members", null);

        Assert.That(request.UserId, Is.EqualTo("alice"));
        Assert.That(request.Goal, Is.EqualTo("List team members"));
    }
}
