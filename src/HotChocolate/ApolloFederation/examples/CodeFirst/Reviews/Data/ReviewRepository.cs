namespace Reviews;

public class ReviewRepository
{
    private readonly Dictionary<string, Review> _reviews;

    public ReviewRepository()
        => _reviews = CreateReviews().ToDictionary(review => review.Id);

    public Task<IEnumerable<Review>> GetByUserIdAsync(string userId)
        => Task.FromResult(_reviews.Values.Where(review => review.AuthorId == userId));

    public Task<IEnumerable<Review>> GetByProductUpcAsync(string upc)
        => Task.FromResult(_reviews.Values.Where(review => review.Product.Upc == upc));

    public Task<Review> GetByIdAsync(string id)
        => Task.FromResult(_reviews[id]);

    private static IEnumerable<Review> CreateReviews()
    {
        yield return new Review("1", "Love it!", "1", "1");
        yield return new Review("2", "Too expensive.", "1", "2");
        yield return new Review("3", "Could be better.", "2", "3");
        yield return new Review("4", "Prefer something else.", "2", "1");
    }
}
