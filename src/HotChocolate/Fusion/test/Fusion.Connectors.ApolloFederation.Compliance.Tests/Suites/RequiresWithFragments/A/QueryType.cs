using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// Root <c>Query</c> for subgraph <c>a</c>. Exposes <c>a: Entity</c>
/// which returns the second entity (Qux), shareable.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("a")
            .Type<EntityType>()
            .Shareable()
            .Resolve(_ => AData.Entities[1]);
    }
}
