using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Reviews;

public sealed class Product
{
    public Product(int id)
    {
        Id = id;
    }

    [ID<Product>] public int Id { get; }

    public IEnumerable<Review> GetReviews([Service] ReviewRepository repository)
        => repository.GetReviewsByProductId(Id);
}
