using System.Net;
using System.Text;

namespace ApiFirst.LlmOrchestration.McpServer;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            var options = McpServerOptions.Parse(args);
            if (options.Help)
            {
                Console.Error.WriteLine(McpUsageText.Help);
                return 0;
            }

            var server = McpServer.CreateDefault(options);
            if (!string.IsNullOrWhiteSpace(options.HttpPrefix))
            {
                await McpHttpRunner.RunAsync(server, options.HttpPrefix, CancellationToken.None).ConfigureAwait(false);
                return 0;
            }

            await server.RunAsync(Console.In, Console.Out, Console.Error, CancellationToken.None).ConfigureAwait(false);
            return 0;
        }
        catch (McpUsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(McpUsageText.Help);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }
}
