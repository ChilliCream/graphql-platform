using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.IncludeSkip.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>c</c> subgraph (<c>@key(fields: "id")</c>). Owns
/// <c>include</c>/<c>skip</c> and the never-called twins, all of which
/// require <c>isExpensive</c> from subgraph <c>b</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.IsExpensive).External().Type<NonNullType<BooleanType>>();

        descriptor
            .Field("include")
            .Type<NonNullType<BooleanType>>()
            .Requires("isExpensive")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.IsExpensive is null)
                {
                    throw new InvalidOperationException("isExpensive is missing.");
                }
                return true;
            });

        descriptor
            .Field("skip")
            .Type<NonNullType<BooleanType>>()
            .Requires("isExpensive")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.IsExpensive is null)
                {
                    throw new InvalidOperationException("isExpensive is missing.");
                }
                return true;
            });

        descriptor
            .Field("neverCalledInclude")
            .Type<NonNullType<BooleanType>>()
            .Requires("isExpensive")
            .Resolve(_ => throw new InvalidOperationException(
                "neverCalledInclude should not be called."));

        descriptor
            .Field("neverCalledSkip")
            .Type<NonNullType<BooleanType>>()
            .Requires("isExpensive")
            .Resolve(_ => throw new InvalidOperationException(
                "neverCalledSkip should not be called."));
    }

    private static Product ResolveById(string id) => new() { Id = id };
}
