using HotChocolate.ApolloFederation;

namespace Reviews;

[ExtendServiceType]
public class Product
{
    public Product(string upc)
    {
        Upc = upc;
    }

    [Key]
    [External]
    public string Upc { get; }

    public Task<IEnumerable<Review>> GetReviews(ReviewRepository repository)
        => repository.GetByProductUpcAsync(Upc);

    [ReferenceResolver]
    public static Product GetByIdAsync(string upc) => new(upc);
}
