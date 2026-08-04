using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.IncludeSkip.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>a</c> subgraph (<c>@key(fields: "id")</c>). Owns <c>price</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Price).Type<NonNullType<FloatType>>();
    }

    private static Product? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var p) ? p : null;
}
