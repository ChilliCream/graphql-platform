using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnion.A;

public sealed class BetaType : ObjectType<Beta>
{
    protected override void Configure(IObjectTypeDescriptor<Beta> descriptor)
    {
        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Name).Type<StringType>();
        descriptor.Field(b => b.Details).Type<StringType>();
    }
}
