using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class DogType
    : ObjectType<Dog>
{
    protected override void Configure(IObjectTypeDescriptor<Dog> descriptor)
    {
        descriptor.Implements<PetType>();
        descriptor.Implements<BeingType>();
        descriptor.Implements<MammalType>();
        descriptor.Implements<CanineType>();
        descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.DoesKnowCommand(default))
            .Argument("dogCommand", a => a.Type<NonNullType<EnumType<DogCommand>>>());
    }
}
