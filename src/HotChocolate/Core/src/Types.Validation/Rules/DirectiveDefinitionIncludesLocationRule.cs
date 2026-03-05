using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// A Directive definition must include at least one DirectiveLocation.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation">
/// Specification
/// </seealso>
public sealed class DirectiveDefinitionIncludesLocationRule : IValidationEventHandler<DirectiveDefinitionEvent>
{
    /// <summary>
    /// Checks that the directive definition includes at least one location.
    /// </summary>
    public void Handle(DirectiveDefinitionEvent @event, ValidationContext context)
    {
        var directiveDefinition = @event.DirectiveDefinition;

        if (directiveDefinition.Locations == 0)
        {
            context.Log.Write(DirectiveDefinitionMissingLocation(directiveDefinition));
        }
    }
}
