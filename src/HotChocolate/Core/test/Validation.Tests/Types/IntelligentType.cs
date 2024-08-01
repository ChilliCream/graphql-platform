using HotChocolate.Types;

namespace HotChocolate.Validation;

public class IntelligentType
    : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Intelligent");
        descriptor.Field("iq").Type<NonNullType<IntType>>();
    }
}
