namespace HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types;

public class Product
{
    public Product(string id, string sku, string package, string variation)
    {
        Id = id;
        Sku = sku;
        Package = package;
        Variation = new(variation);
    }

    public string Id { get; }

    public string? Sku { get; }

    public string? Package { get; }

    public ProductVariation? Variation { get; }

    public ProductDimension? Dimensions { get; } = new("1", 1);

    public User? CreatedBy { get; } = new("support@apollographql.com", 1337);
}
