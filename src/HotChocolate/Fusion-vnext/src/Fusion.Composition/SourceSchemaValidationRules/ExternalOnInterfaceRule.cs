using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@external</c> directive indicates that a field is defined and resolved elsewhere, not in
/// the current schema. In the case of an interface type, fields are abstract - they do not have
/// direct resolutions at the interface level. Instead, each implementing object type provides the
/// concrete field implementations. Marking an interface field with <c>@external</c> is therefore
/// nonsensical, as there is no actual field resolution in the interface itself to “borrow” from
/// another schema. Such usage raises an <c>EXTERNAL_ON_INTERFACE</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-on-Interface">
/// Specification
/// </seealso>
internal sealed class ExternalOnInterfaceRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (type is InterfaceTypeDefinition && field.HasExternalDirective())
        {
            context.Log.Write(ExternalOnInterface(field, type, schema));
        }
    }
}
