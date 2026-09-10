using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;

/// <summary>
/// Apollo Federation descriptor for <c>Toaster</c> in subgraph <c>a</c>.
/// Implements <c>Node</c> and <c>WithWarranty</c>.
/// </summary>
public sealed class ToasterType : ObjectType<Toaster>
{
    protected override void Configure(IObjectTypeDescriptor<Toaster> descriptor)
    {
        descriptor
            .Implements<NodeType>()
            .Implements<WithWarrantyType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.Warranty).Type<IntType>();
    }

    private static Toaster? ResolveById(string id)
        => SubgraphAData.ToastersById.TryGetValue(id, out var t) ? t : null;
}
