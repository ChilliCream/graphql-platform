using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> value type owned by
/// the <c>a</c> subgraph. Mirrors the audit Schema Definition Language (SDL):
/// <c>type Category { details: String }</c>. No <c>@key</c>; the type only
/// flows out via the parent <c>Product.category</c> field.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor.Field(c => c.Details).Type<StringType>();
    }
}
