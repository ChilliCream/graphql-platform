using System.CommandLine;
using static ChilliCream.Nitro.CommandLine.CommandLineResources;

namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The root command of the Nitro CLI.
/// </summary>
internal sealed class NitroRootCommand : Command
{
    public NitroRootCommand() : base("nitro")
    {
        Description = RootCommand_Description;
    }
}
