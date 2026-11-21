using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

public sealed class ComponentBType : ObjectType<ComponentB>
{
    protected override void Configure(IObjectTypeDescriptor<ComponentB> descriptor)
    {
        descriptor.Field(c => c.B).Name("b").Type<StringType>();
        descriptor.Field(c => c.Product).Type<ProductType>();
    }
}
