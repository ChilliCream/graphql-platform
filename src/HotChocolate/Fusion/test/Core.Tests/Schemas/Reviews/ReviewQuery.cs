namespace HotChocolate.Fusion.Schemas.Reviews;

[GraphQLName("Query")]
public sealed class ReviewQuery
{
    public IEnumerable<Review> GetReviews(
        [Service] ReviewRepository repository) =>
        repository.GetReviews();

    public Author GetAuthorById(
        [Service] ReviewRepository repository,
        int id)
        => new Author(id, "some name");

    public Product GetProductById(
        [Service] ReviewRepository repository,
        int upc)
        => new Product(upc);
}
