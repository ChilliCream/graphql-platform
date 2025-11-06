
namespace eShop.Reviews;

public static class ReviewRepository
{
    private static readonly List<Review> s_reviews =
    [
        new() { Id = "1", AuthorId = "1", ProductUpc = "1", Body = "Love it!" },
        new() { Id = "2", AuthorId = "1", ProductUpc = "2", Body = "Great product!" },
        new() { Id = "3", AuthorId = "2", ProductUpc = "3", Body = "Could be better." },
        new() { Id = "4", AuthorId = "2", ProductUpc = "1", Body = "Excellent quality." },
        new() { Id = "5", AuthorId = "3", ProductUpc = "2", Body = "Not bad." },
        new() { Id = "6", AuthorId = "3", ProductUpc = "4", Body = "Highly recommend!" },
        new() { Id = "7", AuthorId = "4", ProductUpc = "5", Body = "Worth the price." },
        new() { Id = "8", AuthorId = "4", ProductUpc = "6", Body = "Amazing!" },
        new() { Id = "9", AuthorId = "5", ProductUpc = "7", Body = "Good value." },
        new() { Id = "10", AuthorId = "5", ProductUpc = "8", Body = "Satisfied." },
        new() { Id = "11", AuthorId = "6", ProductUpc = "9", Body = "Perfect!" }
    ];

    public static IEnumerable<Review> GetByUserId(string authorId)
        => s_reviews.Where(r => r.AuthorId == authorId);

    public static IEnumerable<Review> GetByProductUpc(string upc)
        => s_reviews.Where(r => r.ProductUpc == upc);

    public static Review? GetById(string id)
        => s_reviews.FirstOrDefault(r => r.Id == id);
}
