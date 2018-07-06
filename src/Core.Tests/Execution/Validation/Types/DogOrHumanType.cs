using HotChocolate.Types;

namespace HotChocolate.Execution.Validation
{
    public class DogOrHumanType
        : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("DogOrHuman");
            descriptor.Type<DogType>();
            descriptor.Type<HumanType>();
        }
    }
}
