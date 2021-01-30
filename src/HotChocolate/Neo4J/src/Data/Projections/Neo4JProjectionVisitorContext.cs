using System.Collections.Generic;
using HotChocolate.Data.Projections;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Projections
{
    /// <inheritdoc/>
    public class Neo4JProjectionVisitorContext
        : ProjectionVisitorContext<Neo4JProjectionDefinition>
    {
        /// <inheritdoc/>
        public Neo4JProjectionVisitorContext(
            IResolverContext context,
            IOutputType initialType)
            : base(context, initialType, new Neo4JProjectionScope())
        {
        }

        /// <summary>
        /// The path from the root to the current position in the input object
        /// </summary>
        public Stack<string> Path { get; } = new();

        /// <summary>
        /// A list of already projected fields
        /// </summary>
        public Stack<Neo4JProjectionDefinition> Projections { get; } = new();
    }
}
