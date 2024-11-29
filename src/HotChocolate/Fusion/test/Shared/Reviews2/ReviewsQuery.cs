using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Reviews2;

[GraphQLName("Query")]
public sealed class ReviewsQuery
{
    public Viewer Viewer { get; } = new();

    public IEnumerable<Review> GetReviews(
        [Service] ReviewRepository repository)
        => repository.GetReviews();

    [NodeResolver]
    public Review? GetReviewById(
        [Service] ReviewRepository repository,
        int id)
        => repository.GetReview(id);

    [NodeResolver]
    public User? GetAuthorById(
        [Service] ReviewRepository repository,
        int id)
        => repository.GetAuthor(id);

    public Product? GetProductById(
        [Service] ReviewRepository repository,
        [ID(nameof(Product))] int id)
        => new(id);

    public IReviewOrAuthor GetReviewOrAuthor(
        [Service] ReviewRepository repository)
        => repository.GetReviews().First();
}
