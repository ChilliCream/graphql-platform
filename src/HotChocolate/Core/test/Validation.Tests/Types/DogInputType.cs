using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class DogInputType : InputObjectType
{
    protected override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        descriptor.Name("DogInput");

        descriptor.Field("name").Type("String!");
        descriptor.Field("nickname").Type("String");
        descriptor.Field("barkVolume").Type("Int");
    }
}
