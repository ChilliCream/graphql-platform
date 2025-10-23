using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@external</c> directive indicates that a field is <b>defined</b> in a different source
/// schema, and the current schema merely references it. Therefore, a field marked with
/// <c>@external</c> must <b>not</b> simultaneously carry directives that assume local ownership or
/// resolution responsibility, such as <c>@require</c>, which specifies dependencies on other fields
/// to resolve this field. Since <c>@external</c> fields are not locally resolved, there is no need
/// for <c>@require</c>.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Require-Collision">
/// Specification
/// </seealso>
internal sealed class ExternalRequireCollisionRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        if (field.HasExternalDirective()
            && field.Arguments.AsEnumerable().Any(a => a.HasRequireDirective()))
        {
            context.Log.Write(ExternalRequireCollision(field, schema));
        }
    }
}
