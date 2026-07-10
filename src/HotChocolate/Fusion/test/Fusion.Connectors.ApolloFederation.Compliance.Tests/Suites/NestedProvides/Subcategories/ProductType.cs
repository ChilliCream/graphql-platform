using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>subcategories</c> subgraph. Mirrors the audit SDL
/// <c>type Product @key(fields: "id")</c> with fields <c>id: ID!</c>
/// and <c>categories: [Category] @shareable</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Categories).Shareable().Type<ListType<CategoryEntityType>>();
    }

    private static Product ResolveById(string id)
        => SubcategoriesData.BuildProduct(id);
}
