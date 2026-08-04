using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> value type owned by
/// the <c>b</c> subgraph. Mirrors the audit Schema Definition Language (SDL):
/// <c>type Category { id: ID @shareable }</c>. No <c>@key</c>; the type
/// only flows out via the parent <c>Product.category</c> field, but
/// <c>id</c> is shareable so other subgraphs can produce the same value.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor.Field(c => c.Id).Shareable().Type<IdType>();
    }
}
