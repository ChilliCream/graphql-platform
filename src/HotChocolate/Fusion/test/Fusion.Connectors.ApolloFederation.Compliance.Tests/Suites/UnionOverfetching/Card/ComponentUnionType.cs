using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

public sealed class ComponentUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("Component");
        descriptor.Type<ComponentAType>();
        descriptor.Type<ComponentBType>();
        descriptor.Type<ComponentCType>();
    }
}
