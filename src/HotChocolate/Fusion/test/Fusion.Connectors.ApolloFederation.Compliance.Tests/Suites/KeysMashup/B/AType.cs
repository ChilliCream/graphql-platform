using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.KeysMashup.B;

/// <summary>
/// Apollo Federation descriptor for the <c>A</c> entity in subgraph
/// <c>b</c>. Declares four keys, the <c>id compositeId { two three }</c>
/// key is resolvable. <c>name</c> is external; <c>nameInB</c> uses
/// <c>@requires(fields: "name")</c>.
/// </summary>
public sealed class AType : ObjectType<A>
{
    protected override void Configure(IObjectTypeDescriptor<A> descriptor)
    {
        descriptor.Name("A");

        descriptor.Key("compositeId { one two }", resolvable: false);
        descriptor
            .Key("id compositeId { two three }")
            .ResolveReferenceWith(_ => ResolveByIdAndComposite(default!, default!, default!));
        descriptor.Key("pId", resolvable: false);
        descriptor.Key("id", resolvable: false);

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.PId).Name("pId").Type<NonNullType<IdType>>();
        descriptor.Field(a => a.CompositeId).Name("compositeId").Type<NonNullType<CompositeIDType>>();
        descriptor.Field(a => a.Name).External().Type<NonNullType<StringType>>();

        descriptor
            .Field("nameInB")
            .Type<NonNullType<StringType>>()
            .Requires("name")
            .Resolve(ctx =>
            {
                var entity = ctx.Parent<A>();
                if (entity.Name is not { Length: > 0 } name)
                {
                    throw new InvalidOperationException("A.name was not provided.");
                }
                return $"b.a.nameInB {name}";
            });
    }

    private static A? ResolveByIdAndComposite(
        string id,
        [Map("compositeId.two")] string two,
        [Map("compositeId.three")] string three)
    {
        if (!BData.AById.TryGetValue(id, out var entity))
        {
            return null;
        }

        return new A
        {
            Id = entity.Id,
            PId = entity.PId,
            CompositeId = entity.CompositeId
        };
    }
}
