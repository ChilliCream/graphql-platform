using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class MessageType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Message");

            descriptor.Field("body")
                .Type<NonNullType<StringType>>()
                .Resolver(() => "foo");

            descriptor.Field("sender")
                .Type<NonNullType<StringType>>()
                .Resolver(() => "foo");
        }
    }
}
