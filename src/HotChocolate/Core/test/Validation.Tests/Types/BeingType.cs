using HotChocolate.Types;

namespace HotChocolate.Validation;

public class BeingType
    : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Being");
        descriptor.Field("name").Type<NonNullType<StringType>>();
    }
}
