using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// <para>
/// The <c>@shareable</c> directive is intended to indicate that a field on an <b>object type</b>
/// can be resolved by multiple schemas without conflict. As a result, it is only valid to use
/// <c>@shareable</c> on fields <b>of object types</b> (or on the entire object type itself).
/// </para>
/// <para>
/// Applying <c>@shareable</c> to interface fields is disallowed and violates the valid usage of the
/// directive. This rule prevents schema composition errors and data conflicts by ensuring that
/// <c>@shareable</c> is used only in contexts where shared field resolution is meaningful and
/// unambiguous.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Invalid-Shareable-Usage">
/// Specification
/// </seealso>
internal sealed class InvalidShareableUsageRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (type is MutableInterfaceTypeDefinition && field.HasShareableDirective())
        {
            context.Log.Write(InvalidShareableUsage(field, type, schema));
        }
    }
}
