using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types;

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(t => GetProductById(default!, default!));

        descriptor
            .Field(t => t.Id)
            .ID();

        descriptor
            .Key("sku package")
            .ResolveReferenceWith(t => GetProductByPackage(default!, default!, default!));

        descriptor
            .Key("sku variation { id }")
            .ResolveReferenceWith(t => GetProductByVariation(default!, default!, default!));

        ProvidesDescriptorExtensions.Provides(
                descriptor
                    .Field(t => t.CreatedBy), "totalProductsCreated")
            .Type<NonNullType<UserType>>();
    }

    private static Product? GetProductById(
        string id,
        Data repository)
        => repository.Products.FirstOrDefault(t => t.Id.Equals(id));

    private static Product? GetProductByPackage(
        string sku,
        string package,
        Data repository)
        => repository.Products.FirstOrDefault(
            t => (t.Sku?.Equals(sku) ?? false) &&
                 (t.Package?.Equals(package) ?? false));

    private static Product? GetProductByVariation(
        string sku,
        [Map("variation.id")] string variationId,
        Data repository)
        => repository.Products.FirstOrDefault(
            t => (t.Sku?.Equals(sku) ?? false) &&
                 (t.Variation?.Id.Equals(variationId) ?? false));
}
