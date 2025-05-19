using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@is</c> directive’s <c>field</c> argument must be syntactically valid GraphQL. If
/// the selection map string is malformed (e.g., missing closing braces, unbalanced quotes, invalid
/// tokens), then the schema cannot be composed correctly. In such cases, the error
/// <c>IS_INVALID_SYNTAX</c> is raised.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Is-Invalid-Syntax">
/// Specification
/// </seealso>
internal sealed class IsInvalidSyntaxRule : IEventHandler<IsFieldInvalidSyntaxEvent>
{
    public void Handle(IsFieldInvalidSyntaxEvent @event, CompositionContext context)
    {
        var (isDirective, argument, field, type, schema) = @event;

        context.Log.Write(
            IsInvalidSyntax(
                isDirective,
                argument.Name,
                field.Name,
                type.Name,
                schema));
    }
}
