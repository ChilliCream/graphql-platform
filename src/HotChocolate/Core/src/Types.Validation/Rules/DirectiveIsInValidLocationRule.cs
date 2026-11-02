using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Checks that a directive is applied only in locations its definition declares.
/// </summary>
public sealed class DirectiveIsInValidLocationRule : IValidationEventHandler<DirectiveEvent>
{
    /// <summary>
    /// Checks that a directive is applied only in locations its definition declares.
    /// </summary>
    public void Handle(DirectiveEvent @event, ValidationContext context)
    {
        var (directive, member, location) = @event;

        // DirectiveIsDefinedRule reports directives without a definition, and
        // DirectiveDefinitionIncludesLocationRule reports definitions without locations.
        if (directive.Definition is IMissingDirectiveDefinition
            || directive.Definition.Locations == 0)
        {
            return;
        }

        if ((directive.Definition.Locations & location) != location)
        {
            context.Log.Write(DirectiveInInvalidLocation(directive, member, location));
        }
    }
}
