using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Mutations.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.B;

/// <summary>
/// Root <c>Mutation</c> for the <c>b</c> subgraph. Exposes <c>delete</c>
/// (clears a counter) and the shareable <c>addCategory</c>.
/// </summary>
public sealed class MutationType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Mutation);

        descriptor
            .Field("delete")
            .Argument("requestId", a => a.Type<NonNullType<StringType>>())
            .Type<NonNullType<IntType>>()
            .Resolve(ctx =>
            {
                var state = ctx.Service<MutationsState>();
                var requestId = ctx.ArgumentValue<string>("requestId");
                return state.DeleteNumber(requestId);
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
    }
}
