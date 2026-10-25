using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity owned by the
/// <c>c</c> subgraph. Mirrors the audit Schema Definition Language (SDL):
/// <c>type Category @key(fields: "id") { id: ID, name: String }</c>.
/// The reference resolver synthesizes <c>Category#{id}</c> for any
/// requested id.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<IdType>();
        descriptor.Field(c => c.Name).Type<StringType>();
    }

    private static Category ResolveById(string id)
        => new()
        {
            Id = id,
            Name = $"Category#{id}"
        };
}
