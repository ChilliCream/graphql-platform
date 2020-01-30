using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class PetType
        : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Pet");
            descriptor.Field("name").Type<NonNullType<StringType>>();
        }
    }
}
