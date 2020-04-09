using HotChocolate.Types;

namespace HotChocolate.Validation.Types
{
    public class HumanType
        : ObjectType<Human>
    {
        protected override void Configure(IObjectTypeDescriptor<Human> descriptor)
        {
            descriptor.Interface<SentientType>();
            descriptor.Implements<BeingType>();
            descriptor.Implements<IntelligentType>();
            descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
            descriptor.Field("iq")
                .Type<NonNullType<IntType>>()
                .Resolver(() => "");
            descriptor.Field("pets")
                .Type<ListType<PetType>>()
                .Resolver(() => "");
        }
    }
}
