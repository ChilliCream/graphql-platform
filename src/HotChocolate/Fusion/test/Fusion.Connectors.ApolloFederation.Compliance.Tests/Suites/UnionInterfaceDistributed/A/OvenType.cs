using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;

/// <summary>
/// Apollo Federation descriptor for <c>Oven</c> in subgraph <c>a</c>.
/// Does not implement any interfaces in this subgraph.
/// </summary>
public sealed class OvenType : ObjectType<Oven>
{
    protected override void Configure(IObjectTypeDescriptor<Oven> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(o => o.Id).Type<NonNullType<IdType>>();

        // Oven does not expose warranty in subgraph A's SDL.
        descriptor.Field(o => o.Warranty).Ignore();
    }

    private static Oven? ResolveById(string id)
        => SubgraphAData.OvensById.TryGetValue(id, out var o) ? o : null;
}
