using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections.Optimizers;

public class IsProjectedProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(Selection field)
        => field.DeclaringType is { } objectType
            && objectType.Features.Get<ProjectionTypeFeature>()?.AlwaysProjectedFields.Length > 0;

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        if (!context.TypeContext.Features.TryGet(out ProjectionTypeFeature? feature))
        {
            return selection;
        }

        for (var i = 0; i < feature.AlwaysProjectedFields.Length; i++)
        {
            var alias = "__projection_alias_" + i;
            var fieldName = feature.AlwaysProjectedFields[i];

            // if the field is already in the selection set we do not need to project it
            if (context.TryGetSelection(fieldName, out var otherSelection)
                && otherSelection.Field.Name == fieldName)
            {
                continue;
            }

            // if the field is already added as an alias we do not need to add it
            if (context.TryGetSelection(alias, out otherSelection)
                && otherSelection.Field.Name == fieldName)
            {
                continue;
            }

            var field = context.TypeContext.Fields[fieldName];
            var fieldNode = new FieldNode(
                null,
                new NameNode(fieldName),
                new NameNode(alias),
                [],
                [],
                null);

            var nodesPipeline = context.CompileResolverPipeline(field, fieldNode);

            var compiledSelection = new Selection(
                context.NewSelectionId(),
                alias,
                field,
                [new FieldSelectionNode(fieldNode, 0)],
                [],
                isInternal: true,
                resolverPipeline: nodesPipeline);

            context.AddSelection(compiledSelection);
        }

        return selection;
    }

    public static IsProjectedProjectionOptimizer Create(ProjectionProviderContext context) => new();
}
