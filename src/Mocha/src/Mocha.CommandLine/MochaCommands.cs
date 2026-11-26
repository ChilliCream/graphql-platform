using System.CommandLine;
using ChilliCream.Hosting.CommandLine;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostCommandModule<Mocha.CommandLine.MochaCommands>]

namespace Mocha.CommandLine;

public sealed class MochaCommands : IHostCommandModule
{
    public static void Configure(RootCommand root, IServiceProvider services)
    {
        var mocha = new Command("mocha", "Inspect the message bus.");
        var transports = new Command("transports", "List the configured transports.");
        transports.SetAction(result =>
        {
            var runtime = services.GetRequiredService<IMessagingRuntime>();
            foreach (var transport in runtime.Transports)
            {
                result.InvocationConfiguration.Output.WriteLine(transport.Name);
            }
        });

        mocha.Subcommands.Add(transports);
        root.Subcommands.Add(mocha);
    }
}
