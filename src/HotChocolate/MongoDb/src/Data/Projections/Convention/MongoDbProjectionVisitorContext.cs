using System.Collections.Generic;
using HotChocolate.Data.Projections;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb
{
    public class MongoDbProjectionVisitorContext
        : ProjectionVisitorContext<MongoDbProjectionDefinition>
    {
        public MongoDbProjectionVisitorContext(
            IResolverContext context,
            IOutputType initialType)
            : base(context, initialType, new MongoDbProjectionScope())
        {
        }

        public Stack<string> Path { get; } = new Stack<string>();

        public Stack<MongoDbProjectionDefinition> Projections { get; }
            = new Stack<MongoDbProjectionDefinition>();
    }
}
