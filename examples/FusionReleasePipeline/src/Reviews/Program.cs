using HotChocolate.Types.Relay;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);

public sealed class Query
{
    private static readonly Review[] s_reviews =
    [
        new("r-1", "p-1", 5, "Excellent for long coding sessions."),
        new("r-2", "p-2", 4, "A dependable mug with a good handle.")
    ];

    public IReadOnlyList<Review> GetReviews() => s_reviews;

    public Review? GetReview([ID] string id)
        => s_reviews.FirstOrDefault(review => review.Id == id);
}

public sealed record Review(
    [property: ID] string Id,
    [property: ID] string ProductId,
    int Stars,
    string Commentary);
