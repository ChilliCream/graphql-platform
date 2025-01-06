using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

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
        var (entityType, keyDirective, fieldNode, fieldNamePath, schema) = @event;

        if (fieldNode.Directives.Count != 0)
        {
            context.Log.Write(
                KeyDirectiveInFieldsArgument(
                    entityType.Name,
                    keyDirective,
                    fieldNamePath,
                    schema));
        }
    }
}
