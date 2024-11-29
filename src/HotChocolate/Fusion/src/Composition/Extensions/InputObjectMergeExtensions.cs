using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.MergeExtensions;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// This static class provides extension methods to facilitate merging InputObject types in a Fusion graph.
/// </summary>
internal static class InputObjectMergeExtensions
{
    // This extension method creates a new InputField instance by replacing any
    // named types in the source field's type with the equivalent type in the target
    // schema. This is used to create a new merged field in the target schema.
    public static InputFieldDefinition CreateField(
        this CompositionContext context,
        InputFieldDefinition source,
        SchemaDefinition targetSchema)
    {
        var targetFieldType = source.Type.ReplaceNameType(n => targetSchema.Types[n]);
        var target = new InputFieldDefinition(source.Name, targetFieldType);
        target.MergeDescriptionWith(source);
        target.MergeDeprecationWith(source);
        target.MergeDirectivesWith(source, context);
        target.DefaultValue = source.DefaultValue;
        return target;
    }

    // This extension method merges the source InputField into the target InputField.
    // If the target field does not have a description but the source field does, the
    // description is copied from the source to the target. If the source field is
    // deprecated and the target is not, the deprecation reason and status are copied
    // from the source to the target.
    public static void MergeField(
        this CompositionContext context,
        InputObjectTypeDefinition type,
        InputFieldDefinition source,
        InputFieldDefinition target)
    {
        var mergedInputType = MergeInputType(source.Type, target.Type);

        if (mergedInputType is null)
        {
            context.Log.Write(
                LogEntryHelper.InputFieldTypeMismatch(
                    new SchemaCoordinate(type.Name, source.Name),
                    source,
                    target.Type,
                    source.Type));
            return;
        }

        if(!target.Type.Equals(mergedInputType, TypeComparison.Structural))
        {
            target.Type = mergedInputType;
        }

        target.MergeDescriptionWith(source);
        target.MergeDeprecationWith(source);
        target.MergeDirectivesWith(source, context);
    }
}
