using MongoDB.Bson.Serialization.Attributes;

namespace HotChocolate.Data.MongoDb.Integration.Tests.Models;

public class Movie
{
    [BsonId]
    public long Id { get; set; }

    public string? Title { get; set; }

    public List<Actor>? Genres { get; set; }
}
