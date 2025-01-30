using HotChocolate.Types;

namespace HotChocolate.Fusion.Shared.Reviews2;

[GraphQLName("Subscription")]
public sealed class ReviewsSubscription
{
    public async IAsyncEnumerable<Review> CreateOnNewReviewStream()
    {
        var authors = new[]
        {
            new User(1, "@ada"),
            new User(2, "@complete")
        };

        var reviews = new[]
        {
            new Review(1, authors[0], new Product(1), "Love it!"),
            new Review(2, authors[1], new Product(2), "Too expensive."),
            new Review(3, authors[0], new Product(3), "Could be better."),
            new Review(4, authors[1], new Product(1), "Prefer something else.")
        };

        foreach (var review in reviews)
        {
            await Task.Delay(200);
            yield return review;
        }
    }

    [Subscribe(With = nameof(CreateOnNewReviewStream))]
    public Review OnNewReview([EventMessage] Review review)
        => review;

    public async IAsyncEnumerable<Review> CreateOnNewReviewStreamError()
    {
        await Task.Delay(200);

        ThrowError();

        var authors = new[] { new User(1, "@ada"), new User(2, "@complete"), };
        yield return new Review(1, authors[0], new Product(1), "Love it!");
    }

    private static void ThrowError()
        => throw new GraphQLException("ERROR ON SUBSCRIBE");

    [Subscribe(With = nameof(CreateOnNewReviewStreamError))]
    public Review OnNewReviewError([EventMessage] Review review)
        => review;
}
