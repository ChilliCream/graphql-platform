using HotChocolate.ApolloFederation.Types;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionOverfetching.Product;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("a")
            .Type<NonNullType<StringType>>()
            .Resolve(context => ResolveFor(context, "a"));

        descriptor
            .Field("b")
            .Type<NonNullType<StringType>>()
            .Resolve(context => ResolveFor(context, "b"));

        descriptor
            .Field("c")
            .Type<NonNullType<StringType>>()
            .Resolve(context => ResolveFor(context, "c"));
    }

    private static Product? ResolveById(string id)
        => ProductData.Products.FirstOrDefault(
            product => string.Equals(product.Id, id, StringComparison.Ordinal));

    // Throws when the field is resolved for a product of another kind.
    private static string? ResolveFor(IResolverContext context, string kind)
    {
        var product = context.Parent<Product>();

        if (!string.Equals(product.Kind, kind, StringComparison.Ordinal))
        {
            throw new GraphQLException(
                $"Product.{kind} requested for product \"{product.Id}\" of kind \"{product.Kind}\" (overfetching)");
        }

        return kind switch
        {
            "a" => product.A,
            "b" => product.B,
            _ => product.C
        };
    }
}
