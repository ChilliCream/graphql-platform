using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.B;

/// <summary>
/// The <c>@interfaceObject</c> representation of <c>NodeWithName</c> in the
/// <c>b</c> subgraph (<c>@key(fields: "id")</c>). It contributes a
/// <c>username</c> field that <c>@requires(fields: "name")</c>, where
/// <c>name</c> is external and owned by the <c>a</c> subgraph.
/// </summary>
public sealed class NodeWithNameType : ObjectType<NodeWithName>
{
    protected override void Configure(IObjectTypeDescriptor<NodeWithName> descriptor)
    {
        descriptor.Name("NodeWithName");
        descriptor.InterfaceObject();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!, default));

        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
        descriptor.Field(n => n.Name).External().Type<StringType>();

        descriptor
            .Field("username")
            .Type<StringType>()
            .Requires("name")
            .Resolve(ctx =>
            {
                var node = ctx.Parent<NodeWithName>();
                if (!BData.UsernamesById.TryGetValue(node.Id, out var username))
                {
                    return null;
                }

                if (node.Name is null)
                {
                    throw new InvalidOperationException("Requires field 'name' not provided.");
                }

                return username;
            });
    }

    private static NodeWithName? ResolveById(string id, [Map("name")] string? name)
        => BData.Ids.Contains(id) ? new NodeWithName { Id = id, Name = name } : null;
}
