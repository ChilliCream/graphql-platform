using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// When using <c>@override</c>, the <c>from</c> argument indicates the name of the source schema
/// that originally owns the field. Overriding from the <b>same</b> schema creates a contradiction,
/// as it implies both local and transferred ownership of the field within one schema. If the
/// <c>from</c> value matches the local schema name, it triggers an <c>OVERRIDE_FROM_SELF</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Override-from-Self">
/// Specification
/// </seealso>
internal sealed class OverrideFromSelfRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        var overrideDirective = field.Directives[Override].FirstOrDefault();

        if (overrideDirective?.Arguments[From] is StringValueNode from
            && from.Value == schema.Name)
        {
            context.Log.Write(OverrideFromSelf(overrideDirective, field, type, schema));
        }
    }
}
