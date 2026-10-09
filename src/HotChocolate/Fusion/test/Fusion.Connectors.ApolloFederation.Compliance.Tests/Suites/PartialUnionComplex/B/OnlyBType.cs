using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

public sealed class OnlyBType : ObjectType<OnlyB>
{
    protected override void Configure(IObjectTypeDescriptor<OnlyB> descriptor)
    {
        descriptor.Field(o => o.B).Type<StringType>();
    }
}
