using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Mutations.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.A;

/// <summary>
/// Root <c>Mutation</c> for the <c>a</c> subgraph. Exposes <c>addProduct</c>,
/// <c>multiply</c>, and the shareable <c>addCategory</c>.
/// </summary>
public sealed class MutationType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Mutation);

        descriptor
            .Field("addProduct")
            .Argument("input", a => a.Type<NonNullType<AddProductInputType>>())
            .Type<NonNullType<ProductType>>()
            .Resolve(ctx =>
            {
                var state = ctx.Service<MutationsState>();
                var input = ctx.ArgumentValue<AddProductInput>("input");
                return state.AddProduct(input.Name, input.Price);
            });

        descriptor
            .Field("addCategory")
            .Argument("name", a => a.Type<NonNullType<StringType>>())
            .Argument("requestId", a => a.Type<NonNullType<StringType>>())
            .Type<NonNullType<CategoryType>>()
            .Shareable()
            .Resolve(ctx =>
            {
                var state = ctx.Service<MutationsState>();
                var name = ctx.ArgumentValue<string>("name");
                var requestId = ctx.ArgumentValue<string>("requestId");
                return state.AddCategory(name, requestId);
            });

        descriptor
            .Field("multiply")
            .Argument("by", a => a.Type<NonNullType<IntType>>())
            .Argument("requestId", a => a.Type<NonNullType<StringType>>())
            .Type<NonNullType<IntType>>()
            .Resolve(ctx =>
            {
                var state = ctx.Service<MutationsState>();
                var by = ctx.ArgumentValue<int>("by");
                var requestId = ctx.ArgumentValue<string>("requestId");
                return state.MultiplyNumber(by, requestId);
            });
    }
}
