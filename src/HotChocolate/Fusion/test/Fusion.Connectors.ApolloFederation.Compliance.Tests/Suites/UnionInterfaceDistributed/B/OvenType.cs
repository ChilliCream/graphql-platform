using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.B;

/// <summary>
/// Apollo Federation descriptor for <c>Oven</c> in subgraph <c>b</c>.
/// Implements <c>Node</c> and <c>WithWarranty</c>.
/// </summary>
public sealed class OvenType : ObjectType<Oven>
{
    protected override void Configure(IObjectTypeDescriptor<Oven> descriptor)
    {
        descriptor
            .Implements<NodeType>()
            .Implements<WithWarrantyType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(o => o.Id).Type<NonNullType<IdType>>();
        descriptor.Field(o => o.Warranty).Type<IntType>();
    }

    private static Oven? ResolveById(string id)
        => SubgraphBData.OvensById.TryGetValue(id, out var o) ? o : null;
}
