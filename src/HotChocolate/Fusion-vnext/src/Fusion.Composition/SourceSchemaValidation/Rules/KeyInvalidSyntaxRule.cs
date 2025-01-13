using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

/// <summary>
/// Each <c>@key</c> directive must specify the fields that uniquely identify an entity using a
/// valid GraphQL selection set in its <c>fields</c> argument. If the <c>fields</c> argument string
/// is syntactically incorrect—missing closing braces, containing invalid tokens, or otherwise
/// malformed—it cannot be composed into a valid schema and triggers the <c>KEY_INVALID_SYNTAX</c>
/// error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Invalid-Syntax">
/// Specification
/// </seealso>
internal sealed class KeyInvalidSyntaxRule : IEventHandler<KeyFieldsInvalidSyntaxEvent>
{
    public void Handle(KeyFieldsInvalidSyntaxEvent @event, CompositionContext context)
    {
        var (keyDirective, entityType, schema) = @event;

        context.Log.Write(KeyInvalidSyntax(entityType.Name, keyDirective, schema));
    }
}
