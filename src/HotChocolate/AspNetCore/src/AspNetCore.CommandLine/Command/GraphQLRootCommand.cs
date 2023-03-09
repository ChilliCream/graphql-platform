using System.CommandLine;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// The root command of the GraphQL CLI.
/// </summary>
internal sealed class GraphQLRootCommand : Command
{
    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLRootCommand"/>.
    /// </summary>
    public GraphQLRootCommand() : base("graphql")
    {
        Description = "A command line tool for a GraphQL Server.";
        AddCommand(new SchemaCommand());
    }
}
