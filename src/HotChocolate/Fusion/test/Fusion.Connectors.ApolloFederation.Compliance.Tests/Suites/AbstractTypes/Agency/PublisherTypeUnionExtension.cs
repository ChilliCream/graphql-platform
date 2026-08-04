using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Agency;

public sealed class PublisherTypeUnionExtension : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("PublisherType");
        descriptor.Type<AgencyEntityType>();
        descriptor.Type<GroupEntityType>();
    }
}
