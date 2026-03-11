using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// Enum values have to be an exact match across all source schemas. If an enum value only exists in
/// one source schema, it has to be marked as <c>@inaccessible</c>. Enum members that are marked as
/// <c>@inaccessible</c> are not included in the merged enum type. An enum type with no values is
/// considered empty and invalid.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Empty-Merged-Enum-Type">
/// Specification
/// </seealso>
internal sealed class EmptyMergedEnumTypeRule : IEventHandler<EnumTypeEvent>
{
    public void Handle(EnumTypeEvent @event, CompositionContext context)
    {
        var (enumType, schema) = @event;

        if (enumType.HasFusionInaccessibleDirective())
        {
            return;
        }

        var accessibleValues =
            enumType.Values.AsEnumerable().Where(v => !v.HasFusionInaccessibleDirective());

        if (!accessibleValues.Any())
        {
            context.Log.Write(EmptyMergedEnumType(enumType, schema));
        }
    }
}
