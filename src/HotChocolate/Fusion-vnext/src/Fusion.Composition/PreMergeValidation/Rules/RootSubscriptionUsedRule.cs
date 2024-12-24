using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// This rule enforces that, for any source schema, if a root subscription type is defined, it must
/// be named <c>Subscription</c>. Defining a root subscription type with a name other than
/// <c>Subscription</c> or using a differently named type alongside a type explicitly named
/// <c>Subscription</c> creates inconsistencies in schema design and violates the composite schema
/// specification.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Root-Subscription-Used">
/// Specification
/// </seealso>
internal sealed class RootSubscriptionUsedRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;
        var rootSubscription = schema.SubscriptionType;

        if (rootSubscription is not null
            && rootSubscription.Name != WellKnownTypeNames.Subscription)
        {
            context.Log.Write(RootSubscriptionUsed(schema));
        }

        // An object type named 'Subscription' will be set as the root subscription type if it has
        // not yet been defined, so it's not necessary to check for this type in the absence of a
        // root subscription type.
    }
}
