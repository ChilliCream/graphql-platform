using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Mutations.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by the
/// <c>a</c> subgraph (<c>@key(fields: "id")</c>). Owns <c>name</c> and
/// <c>price</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!, default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Name).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Price).Type<NonNullType<FloatType>>();
    }

    private static Product? ResolveById(string id, [Service] MutationsState state)
    {
        state.InitProducts();
        var product = state.GetProducts().FirstOrDefault(
            p => string.Equals(p.Id, id, StringComparison.Ordinal));

        if (product is null)
        {
            return null;
        }

        // Mirror the audit's behavior: a's _resolveReference deletes the
        // product after returning it. This catches planners that issue
        // redundant entity calls.
        state.DeleteProduct(product.Id);
        return product;
    }
}
