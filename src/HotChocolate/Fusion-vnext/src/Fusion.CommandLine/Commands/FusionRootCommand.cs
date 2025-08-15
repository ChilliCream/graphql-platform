using System.CommandLine;
using static HotChocolate.Fusion.Properties.CommandLineResources;

namespace HotChocolate.Fusion.CommandLine;

/// <summary>
/// The root command of the Fusion CLI.
/// </summary>
internal sealed class FusionRootCommand : Command
{
    public FusionRootCommand() : base("fusion")
    {
        Description = RootCommand_Description;
    }
}
