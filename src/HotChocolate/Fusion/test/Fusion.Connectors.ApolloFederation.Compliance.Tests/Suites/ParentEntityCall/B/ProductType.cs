using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.ParentEntityCall.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCall.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>b</c> subgraph. Mirrors the audit Schema Definition Language (SDL):
/// <c>type Product @key(fields: "id pid") { id: ID!, pid: ID!, category: Category @shareable }</c>.
/// The <c>__resolveReference</c> path matches by both <c>id</c> and
/// <c>pid</c> in the shared seed data.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id pid")
            .ResolveReferenceWith(_ => ResolveByIdAndPid(default!, default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Pid).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Category).Shareable().Type<CategoryType>();
    }

    private static Product? ResolveByIdAndPid(string id, string pid)
    {
        var row = ParentEntityCallData.FindProduct(id, pid);
        if (row is null)
        {
            return null;
        }

        var category = ParentEntityCallData.FindCategory(row.CategoryId);
        return new Product
        {
            Id = row.Id,
            Pid = row.Pid,
            Category = category is null
                ? null
                : new Category { Id = category.Id, Name = category.Name }
        };
    }
}
