using System.CommandLine;
using static System.IO.Path;

namespace HotChocolate.Fusion.CommandLine.Commands;

/// <summary>
/// The root command of the GraphQL CLI.
/// </summary>
internal sealed class RootCommand : Command
{
    /// <summary>
    /// Initializes a new instance of <see cref="RootCommand"/>.
    /// </summary>
    public RootCommand() : base("fusion")
    {
        Description = "A command line tool for a Hot Chocolate Fusion.";

        AddCommand(new ComposeCommand());
        AddCommand(new SubgraphCommand());
    }
}
