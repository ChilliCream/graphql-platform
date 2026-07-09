using HotChocolate.Types;

namespace eShop.Reviews;

[ObjectType]
public sealed class Product
{
    public required string Upc { get; init; }

    public IEnumerable<Review> GetReviews()
        => ReviewRepository.GetByProductUpc(Upc);
}
