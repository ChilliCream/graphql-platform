using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.ParentEntityCall.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCall.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity in the
/// <c>a</c> subgraph. Mirrors the audit Schema Definition Language (SDL):
/// <c>type Category @key(fields: "id") { id: ID!, name: String! @shareable }</c>.
/// The <c>__resolveReference</c> path looks the row up by <c>id</c> in the
/// shared seed data.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Name).Shareable().Type<NonNullType<StringType>>();
    }

    private static Category? ResolveById(string id)
    {
        var row = ParentEntityCallData.FindCategory(id);
        return row is null ? null : new Category { Id = row.Id, Name = row.Name };
    }
}
