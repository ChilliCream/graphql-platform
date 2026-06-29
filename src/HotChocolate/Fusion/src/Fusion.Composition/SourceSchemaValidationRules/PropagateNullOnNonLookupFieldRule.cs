using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@propagateNull</c> directive changes how a null lookup result affects its target entity.
/// It is only meaningful on fields that are also marked as lookup fields. Applying it to a
/// non-lookup field is invalid and raises a PROPAGATE_NULL_ON_NON_LOOKUP_FIELD error.
/// </summary>
internal sealed class PropagateNullOnNonLookupFieldRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        if (field is { HasPropagateNullDirective: true, IsLookup: false })
        {
            context.Log.Write(PropagateNullOnNonLookupField(field, schema));
        }
    }
}
