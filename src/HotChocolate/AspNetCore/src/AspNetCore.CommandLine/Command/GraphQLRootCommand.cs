using System.CommandLine;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The root command of the GraphQL CLI.
/// </summary>
internal sealed class GraphQLRootCommand : RootCommand
{
    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLRootCommand"/>.
    /// </summary>
    public GraphQLRootCommand(SchemaCommand schemaCommand)
    {
        Description = "A command line tool for a GraphQL Server.";

        Subcommands.Add(schemaCommand);
    }
}
