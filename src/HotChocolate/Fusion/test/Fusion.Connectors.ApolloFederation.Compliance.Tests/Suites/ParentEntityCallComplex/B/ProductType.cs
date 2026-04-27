using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>b</c> subgraph. Mirrors the audit Schema Definition Language (SDL):
/// <c>type Product @key(fields: "id") { id: ID @external, category: Category @shareable }</c>.
/// The <c>__resolveReference</c> path returns a <see cref="Category"/>
/// inline whose <c>id</c> is hard-pinned to <c>"3"</c> (matching the
/// audit fixture). Downstream subgraphs that own <c>Category</c> resolve
/// further fields from that key.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).External().Type<IdType>();
        descriptor.Field(p => p.Category).Shareable().Type<CategoryType>();
    }

    private static Product ResolveById(string id)
        => new()
        {
            Id = id,
            Category = new Category { Id = "3" }
        };
}
