using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Checks that a directive that is not repeatable is applied at most once per location.
/// </summary>
public sealed class DirectiveIsUniqueRule : IValidationEventHandler<DirectiveEvent>
{
    /// <summary>
    /// Checks that a directive that is not repeatable is applied at most once per location.
    /// </summary>
    public void Handle(DirectiveEvent @event, ValidationContext context)
    {
        var (directive, member) = @event;

        if (directive.Definition.IsRepeatable || member is not IDirectivesProvider provider)
        {
            return;
        }

        var precedingApplications = 0;

        foreach (var other in provider.Directives)
        {
            if (ReferenceEquals(other, directive))
            {
                break;
            }

            if (other.Name.Equals(directive.Name, StringComparison.Ordinal))
            {
                precedingApplications++;
            }
        }

        // Reported at the second application, so a location reports one entry per directive name.
        if (precedingApplications == 1)
        {
            context.Log.Write(DirectiveNotUnique(directive, member));
        }
    }
}
