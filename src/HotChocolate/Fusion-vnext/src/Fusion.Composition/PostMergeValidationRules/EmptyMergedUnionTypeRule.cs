using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// TODO: Summary
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Empty-Merged-Union-Type">
/// Specification
/// </seealso>
internal sealed class EmptyMergedUnionTypeRule : IEventHandler<UnionTypeEvent>
{
    public void Handle(UnionTypeEvent @event, CompositionContext context)
    {
        var (unionType, schema) = @event;

        if (unionType.HasFusionInaccessibleDirective())
        {
            return;
        }

        var accessibleTypes = unionType.Types.Where(t => !t.HasFusionInaccessibleDirective());

        if (!accessibleTypes.Any())
        {
            context.Log.Write(EmptyMergedUnionType(unionType, schema));
        }
    }
}
