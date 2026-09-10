using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Agency;

public sealed class EmailType : ObjectType<EmailValue>
{
    protected override void Configure(IObjectTypeDescriptor<EmailValue> descriptor)
    {
        descriptor.Name("Email");
        descriptor.Field(e => e.Address).Type<StringType>();
    }
}
