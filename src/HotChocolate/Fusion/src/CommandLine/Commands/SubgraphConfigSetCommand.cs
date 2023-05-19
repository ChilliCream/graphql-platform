using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Commands;

/// <summary>
/// The subgraph config set command
/// </summary>
internal sealed class SubgraphConfigSetCommand : Command
{
    /// <summary>
    /// Initializes a new instance of <see cref="SubgraphConfigSetCommand"/>.
    /// </summary>
    public SubgraphConfigSetCommand() : base("set")
    {
        Description = "Subgraph Config Set commands.";

        AddCommand(new SubgraphConfigSetNameCommand());
        AddCommand(new SubgraphConfigSetHttpCommand());
        AddCommand(new SubgraphConfigSetWebSocketCommand());
    }
}
