using HotChocolate.ApolloFederation;

namespace Products;

public class Product
{
    public Product(string upc, string name, int price, int weight)
    {
        Upc = upc;
        Name = name;
        Price = price;
        Weight = weight;
    }

    [Key]
    public string Upc { get; }

    public string Name { get; }

    public int Price { get; }

    public int Weight { get; }

    [ReferenceResolver]
    public static Product GetByIdAsync(
        string upc,
        ProductRepository productRepository)
        => productRepository.GetById(upc);
}
