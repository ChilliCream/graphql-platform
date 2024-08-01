using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class FurColor : EnumType
{
    protected override void Configure(IEnumTypeDescriptor descriptor)
    {
        descriptor.Value("RED");
        descriptor.Value("BLUE");
        descriptor.Value("GREEN");
    }
}
