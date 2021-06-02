using System.Diagnostics;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Types;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Integration
{
    [ExtendObjectType("Query")]
    public class Queries
    {
        [GraphQLName("actors")]
        [UseNeo4JDatabase(databaseName: "neo4j")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public Neo4JExecutable<Actor> GetActors(
            [ScopedService] IAsyncSession session) =>
            new (session);

        [GraphQLName("movies")]
        [UseNeo4JDatabase(databaseName: "neo4j")]
        [UseProjection(Scope = "Neo4J")]
        [UseFiltering(Scope = "Neo4J")]
        [UseSorting(Scope = "Neo4J")]
        public Neo4JExecutable<Movie> GetMovies(
            [ScopedService] IAsyncSession session) =>
            new (session);
    }
}
