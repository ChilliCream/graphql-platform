using System.CommandLine;
using static HotChocolate.Fusion.Properties.CommandLineResources;

namespace HotChocolate.Fusion.Commands;

/// <summary>
/// The root command of the Fusion CLI.
/// </summary>
internal sealed class RootCommand : Command
{
    public RootCommand() : base("fusion")
    {
        Description = RootCommand_Description;

        AddCommand(new ComposeCommand());
    }
}
