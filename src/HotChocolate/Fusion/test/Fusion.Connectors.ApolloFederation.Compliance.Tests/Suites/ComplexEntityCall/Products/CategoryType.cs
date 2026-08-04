using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity owned by the
/// <c>products</c> subgraph (<c>@key(fields: "id")</c>).
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<StringType>>();
        descriptor.Field(c => c.Tag).Shareable().Type<StringType>();

        descriptor.Field("mainProduct")
            .Type<NonNullType<ProductType>>()
            .Shareable()
            .Resolve(ctx =>
            {
                var category = ctx.Parent<Category>();
                return ProductsData.ById.TryGetValue(category.MainProductId, out var product)
                    ? product
                    : null;
            });

        descriptor.Ignore(c => c.MainProductId);
    }

    private static Category? ResolveById(string id)
        => ProductsData.CategoriesById.TryGetValue(id, out var c) ? c : null;
}
