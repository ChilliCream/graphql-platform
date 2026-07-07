using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.A;

public sealed class OnlyAType : ObjectType<OnlyA>
{
    protected override void Configure(IObjectTypeDescriptor<OnlyA> descriptor)
    {
        descriptor.Field(o => o.A).Type<StringType>();
    }
}
