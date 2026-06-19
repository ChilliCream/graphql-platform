using AfterHotChocolate.Models;

namespace AfterHotChocolate.GraphQL;

[SubscriptionType]
public static partial class Subscription
{
    // Topic name (nameof) must match the one used by ITopicEventSender in AddBook.
    [Subscribe]
    public static Book OnBookAdded([EventMessage] Book book) => book;
}
