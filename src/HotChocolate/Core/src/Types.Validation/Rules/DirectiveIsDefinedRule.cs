using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Features;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Checks that a directive is defined.
/// </summary>
public sealed class DirectiveIsDefinedRule : IValidationEventHandler<DirectiveEvent>
{
    /// <summary>
    /// Checks that a directive is defined.
    /// </summary>
    public void Handle(DirectiveEvent @event, ValidationContext context)
    {
        var (directive, member) = @event;

        if (DirectiveNames.IsSpecDirective(directive.Name))
        {
            return;
        }

        var feature = directive.Definition.Features.Get<IncompleteDirectiveDefinitionFeature>();

        if (feature?.IsIncomplete == true)
        {
            context.Log.Write(UndefinedDirective(directive, member));
        }
    }
}
