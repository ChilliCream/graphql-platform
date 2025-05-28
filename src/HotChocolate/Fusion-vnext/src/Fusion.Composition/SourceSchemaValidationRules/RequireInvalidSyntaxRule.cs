using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@require</c> directive’s <c>field</c> argument must be syntactically valid GraphQL. If
/// the selection map string is malformed (e.g., missing closing braces, unbalanced quotes, invalid
/// tokens), then the schema cannot be composed correctly. In such cases, the error
/// <c>REQUIRE_INVALID_SYNTAX</c> is raised.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Require-Invalid-Syntax">
/// Specification
/// </seealso>
internal sealed class RequireInvalidSyntaxRule : IEventHandler<RequireFieldInvalidSyntaxEvent>
{
    public void Handle(RequireFieldInvalidSyntaxEvent @event, CompositionContext context)
    {
        var (requireDirective, argument, field, type, schema) = @event;

        context.Log.Write(
            RequireInvalidSyntax(
                requireDirective,
                argument.Name,
                field.Name,
                type.Name,
                schema));
    }
}
