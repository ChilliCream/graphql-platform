using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

internal static class CompositeSchemaBuilderContextExtensions
{
#nullable disable
    public static SelectionSetNode RewriteValueSelectionToSelectionSet(
        this CompositeSchemaBuilderContext context,
        FusionSchemaDefinition schema,
        string declaringTypeName,
        ImmutableArray<IValueSelectionNode> valueSelections)
    {
        var rewriter = context.Features.GetOrSet(static s => new ValueSelectionToSelectionSetRewriter(s), schema);
        return rewriter.Rewrite(valueSelections.Where(t => t is not null), schema.Types[declaringTypeName]);
    }
}
