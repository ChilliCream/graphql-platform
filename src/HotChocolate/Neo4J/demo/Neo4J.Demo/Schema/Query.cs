using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Data.Neo4J;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Types;
using Neo4j.Driver;
using Neo4jDemo.Models;

namespace Neo4jDemo.Schema
{
    [ExtendObjectType(Name = "Query")]
    public class Query
    {
        [UseNeo4JDatabase("neo4j")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public Neo4JExecutable<Business> Businesses([ScopedService] IAsyncSession session) =>
            new (session);
    }
}
