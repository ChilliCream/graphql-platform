namespace Reviews;

public class ReviewRepository
{
    private readonly Dictionary<string, Review> _reviews;

    public ReviewRepository()
    {
        _reviews = CreateReviews().ToDictionary(review => review.Id);
    }

    public IEnumerable<Review> GetByUserId(string userId)
        => _reviews.Values.Where(review => review.AuthorId == userId);

    public IEnumerable<Review> GetByProductUpc(string upc)
        => _reviews.Values.Where(review => review.Product.Upc == upc);

    public Review GetById(string id) => _reviews[id];

    private static IEnumerable<Review> CreateReviews()
    {
        yield return new Review
        {
            Id = "1",
            AuthorId = "1",
            Product = new Product { Upc = "1" },
            Body = "Love it!"
        };

        yield return new Review
        {
            Id = "2",
            AuthorId = "1",
            Product = new Product { Upc = "2" },
            Body = "Too expensive."
        };

        yield return new Review
        {
            Id = "3",
            AuthorId = "2",
            Product = new Product { Upc = "3" },
            Body = "Could be better."
        };

        yield return new Review
        {
            Id = "4",
            AuthorId = "2",
            Product = new Product { Upc = "1" },
            Body = "Prefer something else."
        };
    }
}
