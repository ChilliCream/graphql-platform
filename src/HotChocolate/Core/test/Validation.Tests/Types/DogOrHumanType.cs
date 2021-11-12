using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

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
