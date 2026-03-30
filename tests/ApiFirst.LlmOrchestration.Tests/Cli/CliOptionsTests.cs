using NUnit.Framework;
using ApiFirst.LlmOrchestration.Cli;

namespace ApiFirst.LlmOrchestration.Tests.Cli;

public sealed class CliOptionsTests
{
    [Test]
    public void Parse_reads_required_arguments_and_inferrs_api_base_url()
    {
        var options = CliOptions.Parse(new[]
        {
            "--user-id", "alice",
            "--swagger-url", "http://localhost:5000/api/swagger.json",
            "--goal", "Inspect team member"
        });

        Assert.That(options.Help, Is.False);
        Assert.That(options.ListOperations, Is.False);
        Assert.That(options.UserId, Is.EqualTo("alice"));
        Assert.That(options.SwaggerUrl, Is.EqualTo("http://localhost:5000/api/swagger.json"));
        Assert.That(options.SwaggerFile, Is.Null);
        Assert.That(options.Goal, Is.EqualTo("Inspect team member"));
        Assert.That(options.ApiBaseUrl, Is.EqualTo("http://localhost:5000"));
    }

    [Test]
    public void Parse_supports_help_mode()
    {
        var options = CliOptions.Parse(new[] { "--help" });

        Assert.That(options.Help, Is.True);
        Assert.That(options.UserId, Is.Null);
    }

    [Test]
    public void Parse_supports_offline_swagger_file_mode()
    {
        var options = CliOptions.Parse(new[]
        {
            "--user-id", "alice",
            "--swagger-file", ".\\swagger.json",
            "--goal", "Inspect team member",
            "--api-base-url", "http://localhost:5000"
        });

        Assert.That(options.SwaggerFile, Is.EqualTo(@".\swagger.json"));
        Assert.That(options.SwaggerUrl, Is.Null);
        Assert.That(options.ApiBaseUrl, Is.EqualTo("http://localhost:5000"));
    }

    [Test]
    public void Parse_supports_list_operations_mode_without_goal()
    {
        var options = CliOptions.Parse(new[]
        {
            "--list-operations",
            "--user-id", "alice",
            "--swagger-url", "http://localhost:5000/api/swagger.json"
        });

        Assert.That(options.ListOperations, Is.True);
        Assert.That(options.Goal, Is.Null);
    }

    [Test]
    public void Parse_rejects_missing_goal()
    {
        Assert.Throws<CliUsageException>(() => CliOptions.Parse(new[]
        {
            "--user-id", "alice",
            "--swagger-url", "http://localhost:5000/api/swagger.json"
        }));
    }

    [Test]
    public void Parse_rejects_when_both_swagger_sources_are_present()
    {
        Assert.Throws<CliUsageException>(() => CliOptions.Parse(new[]
        {
            "--user-id", "alice",
            "--swagger-url", "http://localhost:5000/api/swagger.json",
            "--swagger-file", ".\\swagger.json",
            "--goal", "Inspect team member"
        }));
    }
}
