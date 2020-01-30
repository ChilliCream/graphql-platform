using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class DogType
        : ObjectType<Dog>
    {
        protected override void Configure(IObjectTypeDescriptor<Dog> descriptor)
        {
            descriptor.Interface<PetType>();
            descriptor.Field(t => t.Name)
                .Type<NonNullType<StringType>>();
            descriptor.Field(t => t.DoesKnowCommand(default))
                .Argument("dogCommand", a => a.Type<NonNullType<EnumType<DogCommand>>>());
        }
    }
}
