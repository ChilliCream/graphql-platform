using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@external</c> directive indicates that a field is <b>defined</b> in a different source
/// schema, and the current schema merely references it. Therefore, a field marked with
/// <c>@external</c> must <b>not</b> simultaneously carry directives that assume local ownership or
/// resolution responsibility, such as <c>@provides</c>, which declares that the field can supply
/// additional nested fields from the local schema, conflicting with the notion of an external field
/// whose definition resides elsewhere.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Provides-Collision">
/// Specification
/// </seealso>
internal sealed class ExternalProvidesCollisionRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        if (field is { HasExternalDirective: true, HasProvidesDirective: true })
        {
            context.Log.Write(ExternalProvidesCollision(field, schema));
        }
    }
}
