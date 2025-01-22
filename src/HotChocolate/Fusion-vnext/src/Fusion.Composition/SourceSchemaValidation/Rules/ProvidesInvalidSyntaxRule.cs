using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

/// <summary>
/// The <c>@provides</c> directive’s <c>fields</c> argument must be a syntactically valid selection
/// set string, as if you were selecting fields in a GraphQL query. If the selection set is
/// malformed (e.g., missing braces, unbalanced quotes, or invalid tokens), the schema composition
/// fails with a <c>PROVIDES_INVALID_SYNTAX</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Provides-Invalid-Syntax">
/// Specification
/// </seealso>
internal sealed class ProvidesInvalidSyntaxRule : IEventHandler<ProvidesFieldsInvalidSyntaxEvent>
{
    public void Handle(ProvidesFieldsInvalidSyntaxEvent @event, CompositionContext context)
    {
        var (providesDirective, field, type, schema) = @event;

        context.Log.Write(
            ProvidesInvalidSyntax(
                providesDirective,
                field.Name,
                type.Name,
                schema));
    }
}
