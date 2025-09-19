using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class PetInputType : InputObjectType
{
    protected override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        descriptor.Name("PetInput").OneOf();

        descriptor.Field("cat").Type<CatInputType>();
        descriptor.Field("dog").Type<DogInputType>();
    }
}
