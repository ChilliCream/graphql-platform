using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class HumanOrAlienType
    : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("HumanOrAlien");
        descriptor.Type<HumanType>();
        descriptor.Type<AlienType>();
    }
}
