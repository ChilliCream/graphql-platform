using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased.Types;

[Key("id")]
[Key("sku package")]
[Key("sku variation { id }")]
public class Product(string id, string sku, string package, string variation)
{
    [ID]
    public string Id { get; } = id;

    public string? Sku { get; } = sku;

    public string? Package { get; } = package;

    public ProductVariation? Variation { get; } = new(variation);

    public ProductDimension? Dimensions { get; } = new("1", 1);

    [Provides("totalProductsCreated")]
    public User? CreatedBy { get; } = new("contact@chillicream.com", 1337);

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
