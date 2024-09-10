using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class CatInputType : InputObjectType
{
    protected override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        descriptor.Name("CatInput");

        descriptor.Field("name").Type("String!");
        descriptor.Field("nickname").Type("String");
        descriptor.Field("meowVolume").Type("Int");
    }
}
