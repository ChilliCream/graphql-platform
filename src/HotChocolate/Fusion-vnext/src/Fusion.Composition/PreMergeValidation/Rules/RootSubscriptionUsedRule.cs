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

        if (rootSubscription is not null)
        {
            if (rootSubscription.Name != WellKnownTypeNames.Subscription)
            {
                context.Log.Write(RootSubscriptionUsed(schema));
            }
        }
        else
        {
            var namedSubscriptionType =
                schema.Types.FirstOrDefault(t => t.Name == WellKnownTypeNames.Subscription);

            if (namedSubscriptionType is not null)
            {
                context.Log.Write(RootSubscriptionUsed(schema));
            }
        }
    }
}
