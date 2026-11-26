using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Features;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// The previous directive of a directive extension must already be defined.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2026/#sec-Directive-Extensions.Type-Validation">
/// Specification
/// </seealso>
public sealed class DirectiveExtensionTargetIsDefinedRule
    : IValidationEventHandler<DirectiveDefinitionEvent>
{
    /// <summary>
    /// Checks that the directive definition is not an extension whose directive is undefined.
    /// </summary>
    public void Handle(DirectiveDefinitionEvent @event, ValidationContext context)
    {
        var directiveDefinition = @event.DirectiveDefinition;

        // The parser marks a directive definition only when it creates one for an
        // extension whose directive has no definition.
        if (directiveDefinition.Features.Get<TypeExtensionMarker>() is not null)
        {
            context.Log.Write(DirectiveExtensionUnknownTarget(directiveDefinition));
        }
    }
}
