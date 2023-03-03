using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

public sealed class InputObjectTypeMergeHandler : ITypeMergeHandler
{
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.InputObject))
        {
            return new(MergeStatus.Skipped);
        }

        var target = (InputObjectType)context.FusionGraph.Types[typeGroup.Name];

        foreach (var part in typeGroup.Parts)
        {
            var source = (InputObjectType)part.Type;
            MergeType(context, source, part.Schema, target, context.FusionGraph);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        InputObjectType source,
        Schema sourceSchema,
        InputObjectType target,
        Schema targetSchema)
    {
        context.TryApplySource(source, sourceSchema, target);

        if (string.IsNullOrEmpty(target.Description))
        {
            target.Description = source.Description;
        }

        foreach (var sourceField in source.Fields)
        {
            if (target.Fields.TryGetField(sourceField.Name, out var targetField))
            {
                context.MergeField(sourceField, targetField);
            }
            else
            {
                targetField = context.CreateField(sourceField, targetSchema);
                target.Fields.Add(targetField);
            }

            context.TryApplySource(sourceField, sourceSchema, targetField);
        }
    }
}
