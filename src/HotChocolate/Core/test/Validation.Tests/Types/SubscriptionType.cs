using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class SubscriptionType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Subscription");

        descriptor.Field("newMessage")
            .Type<NonNullType<MessageType>>()
            .Resolve(() => "foo");

        descriptor.Field("disallowedSecondRootField")
            .Type<NonNullType<BooleanType>>()
            .Resolve(() => "foo");

        descriptor.Field("disallowedThirdRootField")
            .Type<NonNullType<BooleanType>>()
            .Resolve(() => "foo");

        descriptor.Field("listEvent")
            .Type<NonNullType<ListType<BooleanType>>>()
            .Resolve(() => "foo");
    }
}
