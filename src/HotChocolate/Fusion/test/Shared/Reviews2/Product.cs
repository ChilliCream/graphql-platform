namespace HotChocolate.Fusion.Shared.Reviews2;

public sealed class Product
{
    public Product(int upc)
    {
        Upc = upc;
    }

    public int Upc { get; }

    public IEnumerable<Review> GetReviews([Service] ReviewRepository repository)
        => repository.GetReviewsByProductId(Upc);
}
