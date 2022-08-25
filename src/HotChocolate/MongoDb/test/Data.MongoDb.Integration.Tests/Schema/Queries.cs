using HotChocolate.Data.MongoDb.Integration.Tests.Models;
using HotChocolate.Types;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Integration.Tests.Schema;

[ExtendObjectType("Query")]
public class Queries
{
    [GraphQLName("actors")]
    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IExecutable<Actor> GetActors(
        [Service] IMongoCollection<Actor> collection) =>
        collection.AsExecutable();

    [GraphQLName("movies")]
    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IExecutable<Movie> GetMovies(
        [Service] IMongoCollection<Movie> collection) =>
        collection.AsExecutable();
}
