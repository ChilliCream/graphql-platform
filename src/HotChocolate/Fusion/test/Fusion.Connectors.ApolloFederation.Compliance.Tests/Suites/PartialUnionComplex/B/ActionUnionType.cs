using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

public sealed class ActionUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("Action");
        descriptor.Type<CommonType>();
        descriptor.Type<OnlyBType>();
    }
}
