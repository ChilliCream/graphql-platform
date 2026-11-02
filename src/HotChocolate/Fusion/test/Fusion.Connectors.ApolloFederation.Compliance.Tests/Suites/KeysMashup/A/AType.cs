using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.KeysMashup.A;

/// <summary>
/// Apollo Federation descriptor for the <c>A</c> entity in subgraph
/// <c>a</c>. Declares four keys (id, pId, composite, deeply composite),
/// only the <c>id</c> key is resolvable; the other keys exist solely so
/// the planner can recognize them on the supergraph side.
/// </summary>
public sealed class AType : ObjectType<A>
{
    protected override void Configure(IObjectTypeDescriptor<A> descriptor)
    {
        descriptor.Name("A");

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Key("pId", resolvable: false);
        descriptor.Key("compositeId { one two }", resolvable: false);
        descriptor.Key("id compositeId { two three }", resolvable: false);

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.PId).Name("pId").Type<NonNullType<IdType>>();
        descriptor.Field(a => a.CompositeId).Name("compositeId").Type<NonNullType<CompositeIDType>>();
        descriptor.Field(a => a.Name).Type<NonNullType<StringType>>();
    }

    private static A? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var a) ? a : null;
}
