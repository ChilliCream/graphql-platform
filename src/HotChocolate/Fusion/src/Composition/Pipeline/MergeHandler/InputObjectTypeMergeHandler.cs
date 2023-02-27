using HotChocolate.Fusion.Composition.Types;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

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
            MergeType(context, source, target, context.FusionGraph);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        InputObjectType source,
        InputObjectType target,
        Schema targetSchema)
    {
        if (string.IsNullOrEmpty(target.Description))
        {
            target.Description = source.Description;
        }

        foreach (var sourceField in source.Fields)
        {
            if (target.Fields.TryGetField(sourceField.Name, out var targetField))
            {
                if (sourceField.IsDeprecated && string.IsNullOrEmpty(targetField.DeprecationReason))
                {
                    targetField.DeprecationReason = sourceField.DeprecationReason;
                    targetField.IsDeprecated = sourceField.IsDeprecated;
                }
            }
            else
            {
                var targetFieldType = sourceField.Type.ReplaceNameType(n => targetSchema.Types[n]);
                targetField = new InputField(sourceField.Name, targetFieldType);
                targetField.DeprecationReason = sourceField.DeprecationReason;
                targetField.IsDeprecated = sourceField.IsDeprecated;
                target.Fields.Add(targetField);
            }
        }
    }
}
