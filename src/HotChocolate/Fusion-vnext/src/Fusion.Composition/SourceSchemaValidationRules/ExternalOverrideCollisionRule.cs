using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@external</c> directive indicates that a field is <b>defined</b> in a different source
/// schema, and the current schema merely references it. Therefore, a field marked with
/// <c>@external</c> must <b>not</b> simultaneously carry directives that assume local ownership or
/// resolution responsibility, such as <c>@override</c>, which transfers ownership of the field’s
/// definition from one schema to another.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Override-Collision">
/// Specification
/// </seealso>
internal sealed class ExternalOverrideCollisionRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.HasExternalDirective() && field.HasOverrideDirective())
        {
            context.Log.Write(ExternalOverrideCollision(field, type, schema));
        }
    }
}
