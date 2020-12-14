using System.Collections.Generic;
using HotChocolate.Data.Projections;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb
{
    /// <inheritdoc/>
    public class MongoDbProjectionVisitorContext
        : ProjectionVisitorContext<MongoDbProjectionDefinition>
    {
        public MongoDbProjectionVisitorContext(
            IResolverContext context,
            IOutputType initialType)
            : base(context, initialType, new MongoDbProjectionScope())
        {
        }

        /// <summary>
        /// The path from the root to the current position in the input object
        /// </summary>
        public Stack<string> Path { get; } = new Stack<string>();

        /// <summary>
        /// A list of already projected fields
        /// </summary>
        public Stack<MongoDbProjectionDefinition> Projections { get; } =
            new Stack<MongoDbProjectionDefinition>();
    }
}
