using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.D;

/// <summary>
/// Root <c>Query</c> type for the <c>d</c> subgraph. Exposes
/// <c>productFromD(id: ID!): Product</c>; the resolver synthesizes a
/// <see cref="Product"/> for the supplied id.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("productFromD")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Type<ProductType>()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<string>("id");
                return new Product { Id = id, Name = $"Product#{id}" };
            });
    }
}
