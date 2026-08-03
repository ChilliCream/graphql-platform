using HotChocolate.ApolloFederation.Types;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Entity</c> type in subgraph <c>b</c>.
/// <c>data</c> is external. <c>requirer</c> requires complex fragment-based
/// field selection on <c>data</c>. <c>requirer2</c> requires
/// <c>data { ... on Foo { foo } }</c>.
/// </summary>
public sealed class EntityType : ObjectType<Entity>
{
    protected override void Configure(IObjectTypeDescriptor<Entity> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!, default));

        descriptor.Field(e => e.Id).Type<NonNullType<IdType>>();
        descriptor.Field(e => e.Data).External().Type<FooInterfaceType>();

        descriptor
            .Field("requirer")
            .Type<NonNullType<StringType>>()
            .Requires("""
                data {
                  foo
                  ... on Bar { bar ... on Baz { baz } ... on Qux { qux } }
                }
                """)
            .Resolve(ctx =>
            {
                var entity = ctx.Parent<Entity>();
                if (entity.Data is not { Foo: { Length: > 0 } foo })
                {
                    throw new InvalidOperationException("Expected entity to have a data field.");
                }

                return foo + "_requirer";
            });

        descriptor
            .Field("requirer2")
            .Type<NonNullType<StringType>>()
            .Requires("""
                data { ... on Foo { foo } }
                """)
            .Resolve(ctx =>
            {
                var entity = ctx.Parent<Entity>();
                if (entity.Data is not { Foo: { Length: > 0 } foo })
                {
                    throw new InvalidOperationException("Expected entity to have a data field.");
                }

                return foo + "_requirer2";
            });
    }

    private static Entity? ResolveById(string id, [Map("data.foo")] string? foo)
    {
        if (!BData.EntitiesById.TryGetValue(id, out var record))
        {
            return null;
        }

        return new Entity
        {
            Id = record.Id,
            Data = foo is null ? null : new RequiredFoo(foo)
        };
    }
}
