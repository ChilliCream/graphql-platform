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
                MergeField(context, sourceField, targetField);
            }
            else
            {
                target.Fields.Add(CreateField(context, sourceField));
            }
        }
    }


    private static OutputField CreateField(
        CompositionContext context,
        OutputField source)
    {
        var fusionGraph = context.FusionGraph;
        var target = new OutputField(source.Name);
        target.Description = source.Description;
        target.Type = source.Type.ReplaceNameType(n => fusionGraph.Types[n]);

        if (source.IsDeprecated)
        {
            target.DeprecationReason = source.DeprecationReason;
            target.IsDeprecated = source.IsDeprecated;
        }

        foreach (var sourceArgument in source.Arguments)
        {
            var targetArgument = new InputField(sourceArgument.Name);
            targetArgument.Description = sourceArgument.Description;
            targetArgument.DefaultValue = sourceArgument.DefaultValue;
            targetArgument.Type = sourceArgument.Type.ReplaceNameType(n => fusionGraph.Types[n]);

            if (sourceArgument.IsDeprecated)
            {
                targetArgument.DeprecationReason = sourceArgument.DeprecationReason;
                targetArgument.IsDeprecated = sourceArgument.IsDeprecated;
            }

            target.Arguments.Add(targetArgument);
        }

        return target;
    }

    private static void MergeField(
        CompositionContext context,
        OutputField source,
        OutputField target)
    {
        if (target.Arguments.Count != source.Arguments.Count)
        {
            // error
        }

        var argMatchCount = 0;

        foreach (var targetArgument in target.Arguments)
        {
            if (source.Arguments.ContainsName(targetArgument.Name))
            {
                argMatchCount++;
            }
        }

        if (argMatchCount != target.Arguments.Count)
        {
            // error
        }

        if (string.IsNullOrEmpty(target.Description))
        {
            target.Description = source.Description;
        }

        if (!target.IsDeprecated && source.IsDeprecated)
        {
            target.DeprecationReason = source.DeprecationReason;
            target.IsDeprecated = source.IsDeprecated;
        }

        foreach (var sourceArgument in source.Arguments)
        {
            var targetArgument = target.Arguments[sourceArgument.Name];

            if (string.IsNullOrEmpty(targetArgument.Description))
            {
                targetArgument.Description = sourceArgument.Description;
            }

            if (!targetArgument.IsDeprecated && sourceArgument.IsDeprecated)
            {
                targetArgument.DeprecationReason = sourceArgument.DeprecationReason;
                targetArgument.IsDeprecated = sourceArgument.IsDeprecated;
            }

            if (sourceArgument.DefaultValue is not null &&
                targetArgument.DefaultValue is null)
            {
                targetArgument.DefaultValue = sourceArgument.DefaultValue;
            }
        }
    }
}
