using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Data.Neo4J.Integration.AnnotationBased.Models;
using HotChocolate.Types;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Integration.AnnotationBased.Schema;

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
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public Neo4JExecutable<Movie> GetMovies(
        [ScopedService] IAsyncSession session) =>
        new (session);
}