using MongoDB.Bson.Serialization.Attributes;

namespace HotChocolate.Data.MongoDb.Integration.Tests.Models;

public class Actor
{
    [BsonId]
    public long Id { get; set; }

    public string? Name { get; set; }

    [UseSorting]
    public List<Movie>? ActedIn { get; set; }

    // [GraphQLName("movies")]
    // [UseProjection]
    // [UseFiltering]
    // [UseSorting]
    // public IExecutable<Movie> GetMovies(
    //     [Service] IMongoCollection<Movie> collection) =>
    //     collection.AsExecutable();
}
