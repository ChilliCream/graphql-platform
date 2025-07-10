using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// For interface types defined across multiple source schemas, the merged interface type is the
/// superset of all fields defined in these source schemas. However, any field marked with
/// <c>@inaccessible</c> in any source schema is hidden and not included in the merged interface
/// type. An interface type with no fields, after considering <c>@inaccessible</c> annotations, is
/// considered empty and invalid.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Empty-Merged-Interface-Type">
/// Specification
/// </seealso>
internal sealed class EmptyMergedInterfaceTypeRule : IEventHandler<InterfaceTypeEvent>
{
    public void Handle(InterfaceTypeEvent @event, CompositionContext context)
    {
        var (interfaceType, schema) = @event;

        if (interfaceType.HasFusionInaccessibleDirective())
        {
            return;
        }

        var accessibleFields =
            interfaceType.Fields.AsEnumerable().Where(f => !f.HasFusionInaccessibleDirective());

        if (!accessibleFields.Any())
        {
            context.Log.Write(EmptyMergedInterfaceType(interfaceType, schema));
        }
    }
}
