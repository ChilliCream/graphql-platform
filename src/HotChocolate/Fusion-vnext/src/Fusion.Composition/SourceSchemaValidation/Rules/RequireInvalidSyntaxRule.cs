using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

/// <summary>
/// The <c>@require</c> directive’s <c>fields</c> argument must be syntactically valid GraphQL. If
/// the selection map string is malformed (e.g., missing closing braces, unbalanced quotes, invalid
/// tokens), then the schema cannot be composed correctly. In such cases, the error
/// <c>REQUIRE_INVALID_SYNTAX</c> is raised.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Require-Invalid-Syntax">
/// Specification
/// </seealso>
internal sealed class RequireInvalidSyntaxRule : IEventHandler<RequireFieldsInvalidSyntaxEvent>
{
    public void Handle(RequireFieldsInvalidSyntaxEvent @event, CompositionContext context)
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
