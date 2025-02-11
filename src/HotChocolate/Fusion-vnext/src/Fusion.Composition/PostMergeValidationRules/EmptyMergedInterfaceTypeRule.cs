using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// TODO: Summary
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Empty-Merged-Interface-Type">
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

        var accessibleFields = interfaceType.Fields.AsEnumerable().Where(f => !f.HasInaccessibleDirective());

        if (!accessibleFields.Any())
        {
            context.Log.Write(EmptyMergedInterfaceType(interfaceType, schema));
        }
    }
}
