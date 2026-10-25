using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphB;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("entity")
            .Type<EntityType>()
            .Provides("name description")
            .Resolve(_ => SubgraphBData.Entities[0]);

        descriptor
            .Field("entities")
            .Type<NonNullType<ListType<NonNullType<EntityType>>>>()
            .Provides("name description")
            .Resolve(_ => SubgraphBData.Entities);
    }
}
