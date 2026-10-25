using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Entity</c> type in subgraph <c>a</c>.
/// Declares <c>@key(fields: "id")</c> and resolves <c>data</c> to Baz or Qux.
/// </summary>
public sealed class EntityType : ObjectType<Entity>
{
    protected override void Configure(IObjectTypeDescriptor<Entity> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(e => e.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("data")
            .Type<FooInterfaceType>()
            .Resolve(ctx =>
            {
                var entity = ctx.Parent<Entity>();
                if (entity.DataId is not { Length: > 0 } dataId)
                {
                    return null;
                }

                return AData.ResolveData(dataId);
            });

        // Hide the DataId property from the schema.
        descriptor.Field(e => e.DataId).Ignore();
    }

    private static Entity? ResolveById(string id)
        => AData.EntitiesById.TryGetValue(id, out var entity) ? entity : null;
}
