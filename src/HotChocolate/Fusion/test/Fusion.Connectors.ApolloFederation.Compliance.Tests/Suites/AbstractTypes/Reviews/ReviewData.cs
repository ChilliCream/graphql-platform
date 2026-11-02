using HotChocolate.Fusion.Suites.AbstractTypes.Products;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Reviews;

public static class ReviewData
{
    public static readonly IReadOnlyList<ReviewEntity> Reviews =
    [
        new ReviewEntity { Id = 1, Body = "review 1", ProductId = "p1", Score = 3 },
        new ReviewEntity { Id = 2, Body = "review 2", ProductId = "p1", Score = 4 },
        new ReviewEntity { Id = 3, Body = "review 3", ProductId = "p2", Score = 5 }
    ];

    public static readonly IReadOnlyDictionary<int, ReviewEntity> ReviewsById =
        Reviews.ToDictionary(static r => r.Id);

    public static IReadOnlyList<ReviewEntity> ReviewsForProduct(string productId)
        => Reviews.Where(r => r.ProductId == productId).ToList();

    public static int ReviewsCountForProduct(string productId)
        => Reviews.Count(r => r.ProductId == productId);

    public static double ReviewsScoreForProduct(string productId)
    {
        var productReviews = Reviews.Where(r => r.ProductId == productId).ToList();

        if (productReviews.Count == 0)
        {
            return 0;
        }

        return productReviews.Average(r => r.Score);
    }

    public static string? GetProductTypeName(string productId)
    {
        if (ProductData.AllProductsById.TryGetValue(productId, out var product))
        {
            return product.TypeName;
        }

        return null;
    }
}

public sealed class ReviewEntity
{
    public int Id { get; init; }
    public string Body { get; init; } = default!;
    public string ProductId { get; init; } = default!;
    public int Score { get; init; }
}
