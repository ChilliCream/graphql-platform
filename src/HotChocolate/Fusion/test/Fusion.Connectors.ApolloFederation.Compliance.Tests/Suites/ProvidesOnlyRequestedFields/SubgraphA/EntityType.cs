using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphA;

public sealed class EntityType(bool punishForPoorPlans) : ObjectType<Entity>
{
    protected override void Configure(IObjectTypeDescriptor<Entity> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(e => e.Id).Type<NonNullType<IdType>>();
        var name = descriptor
            .Field(e => e.Name)
            .Shareable()
            .Type<NonNullType<StringType>>();
        var description = descriptor
            .Field(e => e.Description)
            .Shareable()
            .Type<NonNullType<StringType>>();

        if (punishForPoorPlans)
        {
            name.Resolve(_ => throw new InvalidOperationException(
                "Subgraph 'a' must not resolve Entity.name because subgraph 'b' provided it."));
            description.Resolve(_ => throw new InvalidOperationException(
                "Subgraph 'a' must not resolve Entity.description because subgraph 'b' provided it."));
        }

        descriptor.Field(e => e.Extra).Type<NonNullType<StringType>>();
    }

    private static Entity? ResolveById(string id)
        => SubgraphAData.EntitiesById.GetValueOrDefault(id);
}
