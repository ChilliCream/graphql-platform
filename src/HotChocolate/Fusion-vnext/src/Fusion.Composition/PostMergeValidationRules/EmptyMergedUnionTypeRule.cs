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
internal sealed class EmptyMergedUnionTypeRule : IEventHandler<UnionTypeEvent>
{
    public void Handle(UnionTypeEvent @event, CompositionContext context)
    {
        var (unionType, schema) = @event;

        if (unionType.HasInaccessibleDirective())
        {
            return;
        }

        var accessibleTypes = unionType.Types.Where(f => !f.HasInaccessibleDirective());

        if (!accessibleTypes.Any())
        {
            context.Log.Write(EmptyMergedUnionType(unionType, schema));
        }
    }
}
