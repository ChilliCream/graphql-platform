using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.MergeExtensions;

namespace HotChocolate.Fusion.Composition;

internal static class ComplexTypeMergeExtensions
{
    // This extension method creates a new OutputField by replacing the type name of each field
    // in the source with the corresponding type name in the target schema.
    public static OutputField CreateField(
        this CompositionContext context,
        OutputField source,
        Schema targetSchema)
    {
        var target = new OutputField(source.Name);
        target.MergeDescriptionWith(source);
        target.MergeDeprecationWith(source);

        // Replace the type name of the field in the source with the corresponding type name
        // in the target schema.
        target.Type = source.Type.ReplaceNameType(n => targetSchema.Types[n]);

        // Copy each argument from the source to the target, replacing the type name of each argument
        // in the source with the corresponding type name in the target schema.
        foreach (var sourceArgument in source.Arguments)
        {
            var targetArgument = new InputField(sourceArgument.Name);
            targetArgument.MergeDescriptionWith(sourceArgument);
            targetArgument.DefaultValue = sourceArgument.DefaultValue;

            // Replace the type name of the argument in the source with the corresponding type name
            // in the target schema.
            targetArgument.Type = sourceArgument.Type.ReplaceNameType(n => targetSchema.Types[n]);
            
            targetArgument.MergeDeprecationWith(sourceArgument);

            target.Arguments.Add(targetArgument);
        }

        return target;
    }

    // This extension method merges two OutputFields by copying over their descriptions, deprecation reasons,
    // and arguments (if they have the same name and type). It also logs errors if the arguments have different
    // names or if the number of arguments does not match.
    public static void MergeField(
        this CompositionContext context,
        OutputField source,
        OutputField target,
        string typeName)
    {
        var mergedType = MergeOutputType(source.Type, target.Type);

        if (mergedType is null)
        {
            context.Log.Write(
                LogEntryHelper.OutputFieldTypeMismatch(
                    new SchemaCoordinate(typeName, source.Name),
                    source,
                    target.Type,
                    source.Type));
            return;
        }

        if (!mergedType.Equals(target.Type, TypeComparison.Structural))
        {
            target.Type = mergedType;
        }

        // Log an error if the number of arguments in the source and target fields do not match.
        if (target.Arguments.Count != source.Arguments.Count)
        {
            context.Log.Write(
                LogEntryHelper.OutputFieldArgumentMismatch(
                    new SchemaCoordinate(typeName, source.Name),
                    source));
        }

        var argMatchCount = 0;

        // Count the number of arguments in the target field that have the same name and type as arguments
        // in the source field.
        foreach (var targetArgument in target.Arguments)
        {
            if (source.Arguments.TryGetField(targetArgument.Name, out var sourceArgument))
            {
                argMatchCount++;
                
                var mergedInputType = MergeInputType(sourceArgument.Type, targetArgument.Type);

                if (mergedInputType is null)
                {
                    context.Log.Write(
                        LogEntryHelper.InputFieldTypeMismatch(
                            new SchemaCoordinate(typeName, source.Name, sourceArgument.Name),
                            sourceArgument,
                            sourceArgument.Type,
                            targetArgument.Type));
                    return;
                }
                
                if(!targetArgument.Type.Equals(mergedInputType, TypeComparison.Structural))
                {
                    targetArgument.Type = mergedInputType;
                }
            }
        }

        // Log an error if the number of matching arguments in the target field does not match the total
        // number of arguments in the target field.
        if (argMatchCount != target.Arguments.Count)
        {
            context.Log.Write(
                LogEntryHelper.OutputFieldArgumentSetMismatch(
                    new SchemaCoordinate(typeName, source.Name),
                    source,
                    target.Arguments.Select(t => t.Name).ToArray(),
                    source.Arguments.Select(t => t.Name).ToArray()));
            return;
        }

        // If the target field does not have a description, copy over the description
        // from the source field.
        target.MergeDescriptionWith(source);

        // If the target field is not deprecated and the source field is deprecated, copy over the
        target.MergeDeprecationWith(source);

        foreach (var sourceArgument in source.Arguments)
        {
            var targetArgument = target.Arguments[sourceArgument.Name];

            // If the target argument does not have a description, copy over the description
            // from the source argument.
            targetArgument.MergeDescriptionWith(sourceArgument);

            // If the target argument is not deprecated and the source argument is deprecated,
            targetArgument.MergeDeprecationWith(sourceArgument);

            // If the target argument does not have a default value and the source argument does,
            if (sourceArgument.DefaultValue is not null &&
                targetArgument.DefaultValue is null)
            {
                targetArgument.DefaultValue = sourceArgument.DefaultValue;
            }
        }
    }
}