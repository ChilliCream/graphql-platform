using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class SelfType : ObjectType<SelfPublisher>
{
    protected override void Configure(IObjectTypeDescriptor<SelfPublisher> descriptor)
    {
        descriptor.Name("Self");
        descriptor.Field(s => s.Email).Type<StringType>();
    }
}
