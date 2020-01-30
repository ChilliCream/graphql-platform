using HotChocolate.Types;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class SubscriptionType
        : ObjectType<Subscription>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Subscription> descriptor)
        {
            descriptor.Field(t => t.OnCreateReview(default, default))
                .Type<NonNullType<ReviewType>>();
        }
    }
}
