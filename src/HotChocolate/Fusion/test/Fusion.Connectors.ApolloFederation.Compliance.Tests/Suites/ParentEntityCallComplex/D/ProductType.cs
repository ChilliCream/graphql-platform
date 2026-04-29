using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.D;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by the
/// <c>d</c> subgraph. Mirrors the audit Schema Definition Language (SDL):
/// <c>type Product @key(fields: "id") { id: ID, name: String }</c>.
/// The reference resolver synthesizes <c>Product#{id}</c> for any
/// requested id.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<IdType>();
        descriptor.Field(p => p.Name).Type<StringType>();
    }

    private static Product ResolveById(string id)
        => new()
        {
            Id = id,
            Name = $"Product#{id}"
        };
}
