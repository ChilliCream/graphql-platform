using System.Linq;
using HotChocolate.Types.Relay;

namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased.Types;

[Key("id")]
[Key("sku package")]
[Key("sku variation { id }")]
public class Product
{
    public Product(string id, string sku, string package, string variation)
    {
        Id = id;
        Sku = sku;
        Package = package;
        Variation = new(variation);
    }

    [ID]
    public string Id { get; }

    public string? Sku { get; }

    public string? Package { get; }

    public ProductVariation? Variation { get; }

    public ProductDimension? Dimensions { get; } = new("1", 1);

    [Provides("totalProductsCreated")]
    public User? CreatedBy { get; } = new("support@apollographql.com", 1337);

    [ReferenceResolver]
    public static Product? GetProductById(
        string id,
        Data repository)
        => repository.Products.FirstOrDefault(t => t.Id.Equals(id));

    [ReferenceResolver]
    public static Product? GetProductByPackage(
        string sku,
        string package,
        Data repository)
        => repository.Products.FirstOrDefault(
            t => (t.Sku?.Equals(sku) ?? false) &&
                (t.Package?.Equals(package) ?? false));

    [ReferenceResolver]
    public static Product? GetProductByVariation(
        string sku,
        [Map("variation.id")] string variationId,
        Data repository)
        => repository.Products.FirstOrDefault(
            t => (t.Sku?.Equals(sku) ?? false) &&
                (t.Variation?.Id.Equals(variationId) ?? false));
}
