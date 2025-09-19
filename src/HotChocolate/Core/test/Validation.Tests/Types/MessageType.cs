using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class MessageType
    : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Message");

        descriptor.Field("body")
            .Type<NonNullType<StringType>>()
            .Resolve(() => "foo");

        descriptor.Field("sender")
            .Type<NonNullType<StringType>>()
            .Resolve(() => "foo");
    }
}
