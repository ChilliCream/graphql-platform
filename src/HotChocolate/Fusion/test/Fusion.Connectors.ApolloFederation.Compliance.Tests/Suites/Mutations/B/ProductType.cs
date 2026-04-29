using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Mutations.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>b</c> subgraph (<c>@key(fields: "id")</c>). Owns
/// <c>isExpensive</c> (which uses <c>@requires(fields: "price")</c>) and
/// <c>isAvailable</c>; <c>price</c> is external.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!, default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Price).External().Type<NonNullType<FloatType>>();
        descriptor.Field(p => p.IsAvailable).Type<NonNullType<BooleanType>>();

        descriptor
            .Field("isExpensive")
            .Type<NonNullType<BooleanType>>()
            .Requires("price")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.Price is not double price)
                {
                    throw new InvalidOperationException("Price is not available.");
                }

                return price > 100d;
            });
    }

    private static Product? ResolveById(string id, [Service] MutationsState state)
    {
        var product = state.GetProducts().FirstOrDefault(
            p => string.Equals(p.Id, id, StringComparison.Ordinal));

        if (product is null)
        {
            return null;
        }

        // Mirror the audit's behavior: b's _resolveReference deletes the
        // product after returning it.
        state.DeleteProduct(product.Id);

        return new Product
        {
            Id = product.Id,
            Price = product.Price,
            IsAvailable = true
        };
    }
}
