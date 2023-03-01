using HotChocolate.Fusion.Composition.Types;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

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
            MergeType(context, source, target);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        InterfaceType source,
        InterfaceType target)
    {
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
                target.Fields.Add(context.CreateField(sourceField));
            }
        }
    }
}
