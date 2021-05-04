using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Types;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Integration
{
    [ExtendObjectType(Name = "Query")]
    public class Queries
    {
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public Neo4JExecutable<Actor> Actors([ScopedService] IAsyncSession session) => new (session);
    }
}
