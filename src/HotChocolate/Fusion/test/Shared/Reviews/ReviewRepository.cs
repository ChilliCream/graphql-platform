namespace HotChocolate.Fusion.Shared.Reviews;

public class ReviewRepository
{
    private readonly Dictionary<int, Review> _reviews;
    private readonly Dictionary<int, Author> _authors;

    public ReviewRepository()
    {
        _authors = new[]
        {
            new Author(1, "@ada"),
            new Author(2, "@alan")
        }.ToDictionary(t => t.Id);

        _reviews = new[]
        {
            new Review(1, _authors[1], new Product(1), "Love it!"),
            new Review(2, _authors[2], new Product(2), "Too expensive."),
            new Review(3, _authors[1], new Product(3), "Could be better."),
            new Review(4, _authors[2], new Product(1), "Prefer something else.")
        }.ToDictionary(t => t.Id);
    }

    public IEnumerable<Review> GetReviews() =>
        _reviews.Values.OrderBy(t => t.Id);

    public IEnumerable<Review> GetReviewsByProductId(int upc) =>
        _reviews.Values.OrderBy(t => t.Id).Where(t => t.Product.Id == upc);

    public IEnumerable<Review> GetReviewsByAuthorId(int authorId) =>
        _reviews.Values.OrderBy(t => t.Id).Where(t => t.Author.Id == authorId);

    public Review? GetReview(int id)
        => _reviews.TryGetValue(id, out var review)
            ? review
            : null;

    public Author? GetAuthor(int id)
        => _authors.TryGetValue(id, out var author)
            ? author
            : null;
}
