using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SharedRoot.Category;

/// <summary>
/// Descriptor for the <c>Category</c> value type owned by the <c>category</c>
/// subgraph.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Name).Type<NonNullType<StringType>>();
    }
}
