using HotChocolate.Types;

namespace HotChocolate.Validation.Types
{
    public class CatOrDogType
        : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("CatOrDog");
            descriptor.Type<CatType>();
            descriptor.Type<DogType>();
        }
    }

    public class PetInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("PetInput").OneOf();

            descriptor.Field("cat").Type<CatInputType>();
            descriptor.Field("dog").Type<DogInputType>();
        }
    }

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
}
