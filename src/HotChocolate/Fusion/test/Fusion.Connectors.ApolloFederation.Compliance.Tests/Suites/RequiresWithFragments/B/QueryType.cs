using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// Root <c>Query</c> for subgraph <c>b</c>. Exposes <c>b: Entity</c>
/// (returns entity e1) and <c>bb: Entity</c> (returns entity e2), shareable.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("b")
            .Type<EntityType>()
            .Shareable()
            .Resolve(_ => BData.Entities[0]);

        descriptor
            .Field("bb")
            .Type<EntityType>()
            .Shareable()
            .Resolve(_ => BData.Entities[1]);
    }
}
