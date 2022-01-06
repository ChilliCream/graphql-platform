using System.Collections.Generic;
using HotChocolate;
using HotChocolate.ApolloFederation;
using Reviews.Data;

namespace Reviews.Models;

[ForeignServiceTypeExtension]
public class Product
{
    [Key]
    [External]
    public string Upc { get; set; } = default!;

    public IEnumerable<Review> GetReviews([Service] ReviewRepository reviewRepository)
    {
        return reviewRepository.GetByProductUpc(Upc);
    }
}
