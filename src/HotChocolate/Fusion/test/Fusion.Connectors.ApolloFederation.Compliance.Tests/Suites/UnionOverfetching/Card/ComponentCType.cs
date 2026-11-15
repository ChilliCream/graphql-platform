using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

public sealed class ComponentCType : ObjectType<ComponentC>
{
    protected override void Configure(IObjectTypeDescriptor<ComponentC> descriptor)
    {
        descriptor.Field(c => c.C).Name("c").Type<StringType>();
        descriptor.Field(c => c.Product).Type<ProductType>();
    }
}
