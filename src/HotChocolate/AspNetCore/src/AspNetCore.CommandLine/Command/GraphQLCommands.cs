using System.CommandLine;
using ChilliCream.Hosting.CommandLine;

[assembly: HostCommandModule<HotChocolate.AspNetCore.CommandLine.GraphQLCommands>]

namespace HotChocolate.AspNetCore.CommandLine;

public sealed class GraphQLCommands : IHostCommandModule
{
    public static void Configure(RootCommand root, IServiceProvider services)
        => root.Subcommands.Add(new SchemaCommand(
            new ExportCommand(services),
            new ListCommand(services),
            new PrintCommand(services)));
}
