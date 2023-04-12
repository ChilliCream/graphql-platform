using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Commands;

/// <summary>
/// The subgraph config command
/// </summary>
internal sealed class SubgraphConfigCommand : Command
{
    /// <summary>
    /// Initializes a new instance of <see cref="SubgraphConfigCommand"/>.
    /// </summary>
    public SubgraphConfigCommand() : base("config")
    {
        Description = "Subgraph Config commands.";

        AddCommand(new SubgraphConfigSetCommand());
    }
}
