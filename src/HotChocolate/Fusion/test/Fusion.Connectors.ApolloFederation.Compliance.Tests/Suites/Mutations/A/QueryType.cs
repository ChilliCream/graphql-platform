using HotChocolate.Fusion.Suites.Mutations.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes
/// <c>product(id: ID!): Product!</c> and <c>products: [Product!]!</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Type<NonNullType<ProductType>>()
            .Resolve(ctx =>
            {
                var state = ctx.Service<MutationsState>();
                state.InitProducts();
                var id = ctx.ArgumentValue<string>("id");
                return state.GetProducts().FirstOrDefault(
                    p => string.Equals(p.Id, id, StringComparison.Ordinal));
            });

        descriptor
            .Field("products")
            .Type<NonNullType<ListType<NonNullType<ProductType>>>>()
            .Resolve(ctx =>
            {
                var state = ctx.Service<MutationsState>();
                state.InitProducts();
                return state.GetProducts();
            });
    }
}
