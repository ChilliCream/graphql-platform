using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnion.B;

public sealed class ActionUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("Action");
        descriptor.Type<AlphaType>();
    }
}
