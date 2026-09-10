using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnion.A;

public sealed class ActionUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("Action");
        descriptor.Type<AlphaType>();
        descriptor.Type<BetaType>();
        descriptor.Type<GammaType>();
    }
}
