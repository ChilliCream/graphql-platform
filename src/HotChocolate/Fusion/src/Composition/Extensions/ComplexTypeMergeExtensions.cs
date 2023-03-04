using HotChocolate.Skimmed;

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
        target.Description = source.Description;

        // Replace the type name of the field in the source with the corresponding type name
        // in the target schema.
        target.Type = source.Type.ReplaceNameType(n => targetSchema.Types[n]);

        if (source.IsDeprecated)
        {
            target.DeprecationReason = source.DeprecationReason;
            target.IsDeprecated = source.IsDeprecated;
        }

        // Copy each argument from the source to the target, replacing the type name of each argument
        // in the source with the corresponding type name in the target schema.
        foreach (var sourceArgument in source.Arguments)
        {
            var targetArgument = new InputField(sourceArgument.Name);
            targetArgument.Description = sourceArgument.Description;
            targetArgument.DefaultValue = sourceArgument.DefaultValue;

            // Replace the type name of the argument in the source with the corresponding type name
            // in the target schema.
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

    // This extension method merges two OutputFields by copying over their descriptions, deprecation reasons,
    // and arguments (if they have the same name and type). It also logs errors if the arguments have different
    // names or if the number of arguments does not match.
    public static void MergeField(
        this CompositionContext context,
        OutputField source,
        OutputField target,
        string typeName)
    {
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
            if (source.Arguments.ContainsName(targetArgument.Name))
            {
                argMatchCount++;
            }
        }

        // Log an error if the number of matching arguments in the target field does not match the total
        // number of arguments in the target field.
        if (argMatchCount != target.Arguments.Count)
        {
            context.Log.Write(
                LogEntryHelper.OutputFieldArgumentSetMismatch(
                    new SchemaCoordinate(typeName, source.Name),
                    source));
        }

        // If the target field does not have a description, copy over the description
        // from the source field.
        if (string.IsNullOrEmpty(target.Description))
        {
            target.Description = source.Description;
        }

        // If the target field is not deprecated and the source field is deprecated, copy over the
        if (!target.IsDeprecated && source.IsDeprecated)
        {
            target.DeprecationReason = source.DeprecationReason;
            target.IsDeprecated = source.IsDeprecated;
        }

        foreach (var sourceArgument in source.Arguments)
        {
            var targetArgument = target.Arguments[sourceArgument.Name];

            // If the target argument does not have a description, copy over the description
            // from the source argument.
            if (string.IsNullOrEmpty(targetArgument.Description))
            {
                targetArgument.Description = sourceArgument.Description;
            }

            // If the target argument is not deprecated and the source argument is deprecated,
            if (!targetArgument.IsDeprecated && sourceArgument.IsDeprecated)
            {
                targetArgument.DeprecationReason = sourceArgument.DeprecationReason;
                targetArgument.IsDeprecated = sourceArgument.IsDeprecated;
            }

            // If the target argument does not have a default value and the source argument does,
            if (sourceArgument.DefaultValue is not null &&
                targetArgument.DefaultValue is null)
            {
                targetArgument.DefaultValue = sourceArgument.DefaultValue;
            }
        }
    }
}
