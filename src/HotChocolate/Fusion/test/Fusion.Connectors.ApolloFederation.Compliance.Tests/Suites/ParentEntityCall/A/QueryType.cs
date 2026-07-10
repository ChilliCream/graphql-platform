using HotChocolate.Fusion.Suites.ParentEntityCall.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCall.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes the single
/// <c>products: [Product!]!</c> field used by every audit case to enter
/// the supergraph.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("products")
            .Type<NonNullType<ListType<NonNullType<ProductType>>>>()
            .Resolve(_ => ParentEntityCallData.Products
                .Select(row =>
                {
                    var category = ParentEntityCallData.FindCategory(row.CategoryId);
                    return new Product
                    {
                        Id = row.Id,
                        Pid = row.Pid,
                        Category = category is null
                            ? null
                            : new Category { Id = category.Id, Name = category.Name }
                    };
                })
                .ToArray());
    }
}
