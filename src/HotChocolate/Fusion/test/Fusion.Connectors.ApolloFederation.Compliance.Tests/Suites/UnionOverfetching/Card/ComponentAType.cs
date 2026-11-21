using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

public sealed class ComponentAType : ObjectType<ComponentA>
{
    protected override void Configure(IObjectTypeDescriptor<ComponentA> descriptor)
    {
        descriptor.Field(c => c.A).Name("a").Type<StringType>();
        descriptor.Field(c => c.Product).Type<ProductType>();
    }
}
