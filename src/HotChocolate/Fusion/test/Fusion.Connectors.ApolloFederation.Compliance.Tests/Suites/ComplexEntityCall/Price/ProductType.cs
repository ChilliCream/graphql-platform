using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by the
/// <c>price</c> subgraph
/// (<c>@key(fields: "id pid category { id tag }")</c>). The key resolver
/// extracts the nested <c>category.id</c> / <c>category.tag</c> fields via
/// <see cref="MapAttribute"/>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id pid category { id tag }")
            .ResolveReferenceWith(_ => ResolveByKey(default!, default, default, default));

        descriptor.Field(p => p.Id).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Pid).Type<StringType>();
        descriptor.Field(p => p.Category).Type<CategoryType>();
        descriptor.Field(p => p.Price).Type<PriceType>();
    }

    private static Product ResolveByKey(
        string id,
        [Map("pid")] string? pid,
        [Map("category.id")] string? categoryId,
        [Map("category.tag")] string? categoryTag)
        => PriceData.ResolveByKey(id, pid, categoryId, categoryTag);
}
