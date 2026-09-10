using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCall.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> value type owned
/// by the <c>c</c> subgraph. Mirrors the audit Schema Definition Language
/// (SDL): <c>type Category { details: CategoryDetails }</c>. No <c>@key</c>;
/// the type only flows out via the parent <c>Product.category</c> field.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor.Field(c => c.Details).Type<CategoryDetailsType>();
    }
}

/// <summary>
/// Apollo Federation descriptor for the <c>CategoryDetails</c> value type
/// owned by the <c>c</c> subgraph. Mirrors the audit SDL:
/// <c>type CategoryDetails { products: Int }</c>.
/// </summary>
public sealed class CategoryDetailsType : ObjectType<CategoryDetails>
{
    protected override void Configure(IObjectTypeDescriptor<CategoryDetails> descriptor)
    {
        descriptor.Field(d => d.Products).Type<IntType>();
    }
}
