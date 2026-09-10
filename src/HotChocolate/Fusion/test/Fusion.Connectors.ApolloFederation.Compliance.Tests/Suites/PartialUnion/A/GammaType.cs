using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnion.A;

public sealed class GammaType : ObjectType<Gamma>
{
    protected override void Configure(IObjectTypeDescriptor<Gamma> descriptor)
    {
        descriptor.Field(g => g.Id).Type<NonNullType<IdType>>();
        descriptor.Field(g => g.Label).Type<StringType>();
    }
}
