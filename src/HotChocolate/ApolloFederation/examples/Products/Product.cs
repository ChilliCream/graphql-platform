using HotChocolate.ApolloFederation;

namespace Products;

public class Product
{
    [Key]
    public string Upc { get; set; } = default!;
    
    public string Name { get; set; } = default!;
    
    public int Price { get; set; }

    public int Weight { get; set; }

    [ReferenceResolver]
    public static Product GetAsync(
        string upc,
        ProductRepository productRepository)
        => productRepository.GetById(upc);
}
