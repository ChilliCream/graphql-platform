using HotChocolate.Types;

namespace HotChocolate.Validation.Types
{
    public class HumanType
        : ObjectType<Human>
    {
        protected override void Configure(IObjectTypeDescriptor<Human> descriptor)
        {
            descriptor.Interface<SentientType>();
            descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
            descriptor.Field("pets")
                .Type<ListType<PetType>>()
                .Resolver(() => "");
        }
    }
}
