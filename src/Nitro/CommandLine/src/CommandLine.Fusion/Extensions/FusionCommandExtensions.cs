using System.CommandLine;
using System.CommandLine.Builder;

namespace HotChocolate.Fusion.CommandLine;

public static class FusionCommandExtensions
{
    public static Command AddFusionComposeCommand(this Command command)
    {
        command.AddCommand(new ComposeCommand());

        return command;
    }
}
