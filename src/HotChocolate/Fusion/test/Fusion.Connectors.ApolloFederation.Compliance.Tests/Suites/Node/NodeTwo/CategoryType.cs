using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.NodeTwo;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity in the
/// <c>node-two</c> subgraph (<c>@key(fields: "id")</c>).
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Implements<NodeType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
    }

    private static Category? ResolveById(string id)
        => NodeTwoData.CategoriesById.TryGetValue(id, out var c) ? c : null;
}
