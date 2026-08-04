using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// A Directive definition must not contain the use of a Directive which references
/// itself directly.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation">
/// Specification
/// </seealso>
public sealed class DirectiveDefinitionNoSelfReferenceRule : IValidationEventHandler<DirectiveDefinitionEvent>
{
    /// <summary>
    /// Checks that a directive definition is not applied to itself, either on the
    /// definition or on any of its arguments.
    /// </summary>
    public void Handle(DirectiveDefinitionEvent @event, ValidationContext context)
    {
        var directiveDefinition = @event.DirectiveDefinition;

        if (ContainsSelfReference(directiveDefinition.Directives, directiveDefinition))
        {
            context.Log.Write(DirectiveDefinitionSelfApplication(directiveDefinition));
            return;
        }

        foreach (var argument in directiveDefinition.Arguments)
        {
            if (ContainsSelfReference(argument.Directives, directiveDefinition))
            {
                context.Log.Write(DirectiveDefinitionSelfApplication(directiveDefinition));
                return;
            }
        }
    }

    private static bool ContainsSelfReference(
        IReadOnlyDirectiveCollection directives,
        IDirectiveDefinition directiveDefinition)
    {
        foreach (var directive in directives)
        {
            if (string.Equals(
                directive.Definition.Name,
                directiveDefinition.Name,
                StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
