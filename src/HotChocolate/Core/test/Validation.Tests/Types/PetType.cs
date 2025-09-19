using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class PetType
    : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Pet");
        descriptor.Implements<BeingType>();
        descriptor.Field("name").Type<NonNullType<StringType>>();
    }
}
