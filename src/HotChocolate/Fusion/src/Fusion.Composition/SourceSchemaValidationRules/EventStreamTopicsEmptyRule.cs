using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Validates that <c>@eventStream</c> does not declare an empty list of topics.
/// </summary>
/// <remarks>
/// <para>
/// When the <c>topics</c> argument is omitted, the topics are derived automatically from the field
/// name and its non-cursor arguments. An explicitly empty list (<c>topics: []</c>) is therefore
/// ambiguous and not allowed.
/// </para>
/// </remarks>
internal sealed class EventStreamTopicsEmptyRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        foreach (var directive in field.GetEventStreamDirectives())
        {
            if (directive.Topics is { IsEmpty: true })
            {
                context.Log.Write(EventStreamTopicsEmpty(field, schema));
            }
        }
    }
}
