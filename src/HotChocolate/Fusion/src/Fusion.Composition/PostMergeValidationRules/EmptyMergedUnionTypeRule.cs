using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// For union types defined across multiple source schemas, the merged union type is the union of
/// all member types defined in these source schemas. However, any member type marked with
/// <c>@inaccessible</c> in any source schema is hidden and not included in the merged union type. A
/// union type with no members, after considering <c>@inaccessible</c> annotations, is considered
/// empty and invalid.
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

        var accessibleTypes =
            unionType.Types.AsEnumerable().Where(t => !t.HasFusionInaccessibleDirective());

        if (!accessibleTypes.Any())
        {
            context.Log.Write(EmptyMergedUnionType(unionType, schema));
        }
    }
}
