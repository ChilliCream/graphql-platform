using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal static class ComplexTypeMergeExtensions
{
    public static OutputField CreateField(
        this CompositionContext context,
        OutputField source,
        Schema targetSchema)
    {
        var target = new OutputField(source.Name);
        target.Description = source.Description;
        target.Type = source.Type.ReplaceNameType(n => targetSchema.Types[n]);

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
            targetArgument.Type = sourceArgument.Type.ReplaceNameType(n => targetSchema.Types[n]);

            if (sourceArgument.IsDeprecated)
            {
                targetArgument.DeprecationReason = sourceArgument.DeprecationReason;
                targetArgument.IsDeprecated = sourceArgument.IsDeprecated;
            }

            target.Arguments.Add(targetArgument);
        }

        return target;
    }

    public static void MergeField(
        this CompositionContext context,
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
