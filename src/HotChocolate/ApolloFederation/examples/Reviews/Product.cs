using HotChocolate.ApolloFederation;

namespace Reviews;

[ExtendServiceType]
public class Product
{
    [Key]
    [External]
    public string Upc { get; set; } = default!;

    public IEnumerable<Review> GetReviews(ReviewRepository reviewRepository)
        => reviewRepository.GetByProductUpc(Upc);
}
