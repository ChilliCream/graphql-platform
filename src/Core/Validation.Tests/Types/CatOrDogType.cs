using HotChocolate.Types;

namespace HotChocolate.Validation
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
}
