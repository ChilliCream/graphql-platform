using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

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
internal sealed class KeyInvalidSyntaxRule : IEventHandler<ComplexTypeEvent>
{
    public void Handle(ComplexTypeEvent @event, CompositionContext context)
    {
        var (complexType, schema) = @event;

        foreach (var (keyDirective, keyInfo) in complexType.KeyInfoByDirective)
        {
            if (keyInfo.IsInvalidFieldsSyntax)
            {
                context.Log.Write(KeyInvalidSyntax(complexType, keyDirective, schema));
            }
        }
    }
}
