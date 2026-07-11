using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NestedProvides.Category;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity in the
/// <c>category</c> subgraph. Mirrors the audit SDL
/// <c>type Category @key(fields: "id")</c> with fields <c>id: ID!</c>,
/// <c>name: String</c>, and <c>subCategories: [Category] @external</c>.
/// </summary>
public sealed class CategoryEntityType : ObjectType<CategoryEntity>
{
    protected override void Configure(IObjectTypeDescriptor<CategoryEntity> descriptor)
    {
        descriptor
            .Name("Category")
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Name).Type<StringType>();
        descriptor.Field(c => c.SubCategories).External().Type<ListType<CategoryEntityType>>();
    }

    private static CategoryEntity ResolveById(string id) => new() { Id = id };
}
