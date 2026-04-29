using HotChocolate.Fusion.Suites.Mutations.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.C;

/// <summary>
/// Root <c>Mutation</c> for the <c>c</c> subgraph. Exposes <c>add</c>
/// (increments a counter keyed by <c>requestId</c>).
/// </summary>
public sealed class MutationType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Mutation);

        descriptor
            .Field("add")
            .Argument("num", a => a.Type<NonNullType<IntType>>())
            .Argument("requestId", a => a.Type<NonNullType<StringType>>())
            .Type<NonNullType<IntType>>()
            .Resolve(ctx =>
            {
                var state = ctx.Service<MutationsState>();
                var num = ctx.ArgumentValue<int>("num");
                var requestId = ctx.ArgumentValue<string>("requestId");
                return state.AddNumber(num, requestId);
            });
    }
}
