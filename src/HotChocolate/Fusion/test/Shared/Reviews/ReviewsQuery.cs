namespace HotChocolate.Fusion.Shared.Reviews;

[GraphQLName("Query")]
public sealed class ReviewsQuery
{
    public IEnumerable<Review> GetReviews(
        [Service] ReviewRepository repository) =>
        repository.GetReviews();

    public Author? GetAuthorById(
        [Service] ReviewRepository repository,
        int id)
        => repository.GetAuthor(id);

    public Product? GetProductById(
        [Service] ReviewRepository repository,
        int upc)
        => new Product(upc);
}
