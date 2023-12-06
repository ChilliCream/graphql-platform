using HotChocolate.Types;

namespace HotChocolate.Fusion.Shared.Reviews;

[GraphQLName("Subscription")]
public sealed class ReviewsSubscription
{
    public async IAsyncEnumerable<Review> CreateOnNewReviewStream()
    {
        var authors = new Author[]
        {
            new Author(1, "@ada"),
            new Author(2, "@complete")
        };

        var reviews = new Review[]
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
}
