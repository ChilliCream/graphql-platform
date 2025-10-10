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
public sealed class DirectiveIncludesLocationRule : IValidationEventHandler<DirectiveEvent>
{
    /// <summary>
    /// Checks that the directive definition includes at least one location.
    /// </summary>
    public void Handle(DirectiveEvent @event, ValidationContext context)
    {
        var directive = @event.Directive;

        if (directive.Locations == 0)
        {
            context.Log.Write(DirectiveMissingLocation(directive));
        }
    }
}
