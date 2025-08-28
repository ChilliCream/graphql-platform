using System.CommandLine;
using System.CommandLine.Builder;

namespace HotChocolate.Fusion.CommandLine;

public static class FusionCommandExtensions
{
    public static CommandLineBuilder AddFusion(this CommandLineBuilder builder)
    {
        builder.Command.AddFusionCommands();

        return builder;
    }

    private static Command AddFusionCommands(this Command command)
    {
        command.AddCommand(new ComposeCommand());

        return command;
    }
}
