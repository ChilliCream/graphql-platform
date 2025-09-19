using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Handlers;

public class IsProjectedProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(ISelection field) =>
        field.DeclaringType is ObjectType objectType
        && objectType.Features.Get<ProjectionTypeFeature>()?.AlwaysProjectedFields.Length > 0;

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        if (context.Type is not ObjectType type
            || !type.Features.TryGet(out ProjectionTypeFeature? feature))
        {
            return selection;
        }

        for (var i = 0; i < feature.AlwaysProjectedFields.Length; i++)
        {
            var alias = "__projection_alias_" + i;
            var alwaysProjectedField = feature.AlwaysProjectedFields[i];

            // if the field is already in the selection set we do not need to project it
            if (context.Selections.TryGetValue(alwaysProjectedField, out var field)
                && field.Field.Name == alwaysProjectedField)
            {
                continue;
            }

            // if the field is already added as an alias we do not need to add it
            if (context.Selections.TryGetValue(alias, out field)
                && field.Field.Name == alwaysProjectedField)
            {
                continue;
            }

            var nodesField = type.Fields[alwaysProjectedField];
            var nodesFieldNode = new FieldNode(
                null,
                new NameNode(alwaysProjectedField),
                new NameNode(alias),
                [],
                [],
                null);

            var nodesPipeline = context.CompileResolverPipeline(nodesField, nodesFieldNode);

            var compiledSelection = new Selection.Sealed(
                context.GetNextSelectionId(),
                context.Type,
                nodesField,
                nodesField.Type,
                nodesFieldNode,
                alias,
                arguments: selection.Arguments,
                isInternal: true,
                resolverPipeline: nodesPipeline);

            context.AddSelection(compiledSelection);
        }

        return selection;
    }
}
