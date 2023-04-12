using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Commands;

/// <summary>
/// The subgraph command
/// </summary>
internal sealed class SubgraphCommand : Command
{
    /// <summary>
    /// Initializes a new instance of <see cref="SubgraphCommand"/>.
    /// </summary>
    public SubgraphCommand() : base("subgraph")
    {
        Description = "Subgraph commands.";

        AddCommand(new SubgraphConfigCommand());
        AddCommand(new SubgraphPackCommand());
    }
}
