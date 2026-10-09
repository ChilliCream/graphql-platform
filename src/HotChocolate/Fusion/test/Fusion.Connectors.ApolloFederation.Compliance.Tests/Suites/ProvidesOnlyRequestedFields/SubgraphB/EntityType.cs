using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphB;

public sealed class EntityType(bool punishForPoorPlans) : ObjectType<Entity>
{
    protected override void Configure(IObjectTypeDescriptor<Entity> descriptor)
    {
        var entity = descriptor.Key("id");

        if (punishForPoorPlans)
        {
            entity.ResolveReferenceWith(_ => ThrowForReferenceLookup(default!));
        }
        else
        {
            entity.ResolveReferenceWith(_ => ResolveById(default!));
        }

        descriptor.Field(e => e.Id).Type<NonNullType<IdType>>();
        descriptor.Field(e => e.Name).External().Type<NonNullType<StringType>>();
        var description = descriptor
            .Field(e => e.Description)
            .External()
            .Type<NonNullType<StringType>>();

        if (punishForPoorPlans)
        {
            description.Resolve(_ => throw new InvalidOperationException(
                "Over-fetch detected: subgraph 'b' received unselected Entity.description."));
        }
    }

    private static Entity? ResolveById(string id)
        => SubgraphBData.Entities.FirstOrDefault(entity => entity.Id == id);

    private static Entity? ThrowForReferenceLookup(string id)
        => throw new InvalidOperationException(
            "Subgraph 'b' must only serve entities through Query.entity or Query.entities @provides paths.");
}
