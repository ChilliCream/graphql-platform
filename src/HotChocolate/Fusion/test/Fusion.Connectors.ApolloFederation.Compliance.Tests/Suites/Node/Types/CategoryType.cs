using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.Types;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity in the
/// <c>types</c> subgraph (<c>@key(fields: "id")</c>). Owns <c>name</c>.
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
        descriptor.Field(c => c.Name).Type<NonNullType<StringType>>();
    }

    private static Category? ResolveById(string id)
        => TypesData.CategoriesById.TryGetValue(id, out var c) ? c : null;
}
