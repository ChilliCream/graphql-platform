using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Pipeline;

public sealed class InterfaceTypeMergeHandler : ITypeMergeHandler
{
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Interface))
        {
            return new(MergeStatus.Skipped);
        }

        var target = (InterfaceType)context.FusionGraph.Types[typeGroup.Name];

        foreach (var part in typeGroup.Parts)
        {
            var source = (InterfaceType)part.Type;
            MergeType(context, source, part.Schema, target);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        InterfaceType source,
        Schema sourceSchema,
        InterfaceType target)
    {
        context.TryApplySource(source, sourceSchema, target);

        if (string.IsNullOrEmpty(target.Description))
        {
            source.Description = target.Description;
        }

        foreach (var interfaceType in source.Implements)
        {
            if (!target.Implements.Any(t => t.Name.EqualsOrdinal(interfaceType.Name)))
            {
                target.Implements.Add((InterfaceType)context.FusionGraph.Types[interfaceType.Name]);
            }
        }

        foreach (var sourceField in source.Fields)
        {
            if (target.Fields.TryGetField(sourceField.Name, out var targetField))
            {
                context.MergeField(sourceField, targetField);
            }
            else
            {
                targetField = context.CreateField(sourceField);
                target.Fields.Add(targetField);
            }

            context.TryApplySource(sourceField, sourceSchema, targetField);
        }
    }
}
