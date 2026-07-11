using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity in the
/// <c>subcategories</c> subgraph. Mirrors the audit SDL
/// <c>type Category @key(fields: "id")</c> with fields <c>id: ID!</c>
/// and <c>subCategories: [Category] @shareable</c>.
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
        descriptor.Field(c => c.SubCategories).Shareable().Type<ListType<CategoryEntityType>>();
    }

    private static CategoryEntity ResolveById(string id)
        => SubcategoriesData.BuildCategory(id);
}
