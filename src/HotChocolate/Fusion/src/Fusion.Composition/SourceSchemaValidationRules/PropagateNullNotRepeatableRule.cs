using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@propagateNull</c> directive is not repeatable, so a field must not apply it more than
/// once. Applying it multiple times is invalid and raises a PROPAGATE_NULL_DIRECTIVE_NOT_REPEATABLE
/// error.
/// </summary>
internal sealed class PropagateNullNotRepeatableRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        if (field.HasPropagateNullDirective
            && field.Directives.AsEnumerable().Count(
                d => d.Name == WellKnownDirectiveNames.PropagateNull) > 1)
        {
            context.Log.Write(PropagateNullDirectiveNotRepeatable(field, schema));
        }
    }
}
