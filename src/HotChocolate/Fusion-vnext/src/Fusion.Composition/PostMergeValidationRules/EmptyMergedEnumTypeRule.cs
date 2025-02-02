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
internal sealed class EmptyMergedEnumTypeRule : IEventHandler<EnumTypeEvent>
{
    public void Handle(EnumTypeEvent @event, CompositionContext context)
    {
        var (enumType, schema) = @event;

        if (enumType.HasInaccessibleDirective())
        {
            return;
        }

        var accessibleValues = enumType.Values.Where(f => !f.HasInaccessibleDirective());

        if (!accessibleValues.Any())
        {
            context.Log.Write(EmptyMergedEnumType(enumType, schema));
        }
    }
}
