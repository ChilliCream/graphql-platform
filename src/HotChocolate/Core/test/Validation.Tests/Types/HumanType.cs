using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class HumanType
    : ObjectType<Human>
{
    protected override void Configure(IObjectTypeDescriptor<Human> descriptor)
    {
        descriptor.Implements<SentientType>();
        descriptor.Implements<BeingType>();
        descriptor.Implements<IntelligentType>();
        descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Field("iq")
            .Type<NonNullType<IntType>>()
            .Resolve(() => "");
        descriptor.Field("pets")
            .Type<ListType<PetType>>()
            .Resolve(() => "");
        descriptor.Field("relatives")
            .Type<ListType<HumanType>>()
            .Resolve(() => "");
    }
}
