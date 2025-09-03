using System.CommandLine;
using ChilliCream.Nitro.CommandLine.Fusion.Commands;

namespace ChilliCream.Nitro.CommandLine.Fusion.Extensions;

public static class FusionCommandExtensions
{
    public static Command AddFusionComposeCommand(this Command command)
    {
        command.AddCommand(new ComposeCommand());

        return command;
    }
}
