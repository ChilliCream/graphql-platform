using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// TODO: Summary and spec link
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Empty-Merged-Object-Type">
/// Specification
/// </seealso>
internal sealed class EmptyMergedInterfaceTypeRule : IEventHandler<InterfaceTypeEvent>
{
    public void Handle(InterfaceTypeEvent @event, CompositionContext context)
    {
        var (interfaceType, schema) = @event;

        if (interfaceType.HasInaccessibleDirective())
        {
            return;
        }

        var accessibleFields = interfaceType.Fields.Where(f => !f.HasInaccessibleDirective());

        if (!accessibleFields.Any())
        {
            context.Log.Write(EmptyMergedInterfaceType(interfaceType, schema));
        }
    }
}
