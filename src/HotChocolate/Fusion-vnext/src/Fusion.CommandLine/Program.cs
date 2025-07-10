using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using RootCommand = HotChocolate.Fusion.Commands.RootCommand;

namespace HotChocolate.Fusion;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();

        return await builder.Build().InvokeAsync(args);
    }
}
