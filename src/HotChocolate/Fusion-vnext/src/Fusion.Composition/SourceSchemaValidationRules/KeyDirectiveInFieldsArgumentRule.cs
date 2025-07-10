using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@key</c> directive specifies the set of fields used to uniquely identify an entity. The
/// <c>fields</c> argument must consist of a valid GraphQL selection set that does not include any
/// directive applications. Directives in the <c>fields</c> argument are not supported.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Directive-in-Fields-Argument">
/// Specification
/// </seealso>
internal sealed class KeyDirectiveInFieldsArgumentRule : IEventHandler<KeyFieldNodeEvent>
{
    public void Handle(KeyFieldNodeEvent @event, CompositionContext context)
    {
        var (fieldNode, fieldNamePath, keyDirective, type, schema) = @event;

        if (fieldNode.Directives.Count != 0)
        {
            context.Log.Write(
                KeyDirectiveInFieldsArgument(
                    type.Name,
                    keyDirective,
                    fieldNamePath,
                    schema));
        }
    }
}
