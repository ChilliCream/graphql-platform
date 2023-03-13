// This static class provides extension methods to facilitate merging InputObject types
// in a Fusion graph.

using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal static class InputObjectMergeExtensions
{
    // This extension method creates a new InputField instance by replacing any
    // named types in the source field's type with the equivalent type in the target
    // schema. This is used to create a new merged field in the target schema.
    public static InputField CreateField(
        this CompositionContext context,
        InputField source,
        Schema targetSchema)
    {
        var targetFieldType = source.Type.ReplaceNameType(n => targetSchema.Types[n]);
        var target = new InputField(source.Name, targetFieldType);
        target.DeprecationReason = source.DeprecationReason;
        target.IsDeprecated = source.IsDeprecated;
        target.Description = source.Description;
        return target;
    }

    // This extension method merges the source InputField into the target InputField.
    // If the target field does not have a description but the source field does, the
    // description is copied from the source to the target. If the source field is
    // deprecated and the target is not, the deprecation reason and status are copied
    // from the source to the target.
    public static void MergeField(
        this CompositionContext context,
        InputField source,
        InputField target)
    {
        if (!string.IsNullOrEmpty(source.Description) &&
            string.IsNullOrEmpty(target.Description))
        {
            target.Description = source.Description;
        }

        if (source.IsDeprecated && string.IsNullOrEmpty(target.DeprecationReason))
        {
            target.DeprecationReason = source.DeprecationReason;
            target.IsDeprecated = source.IsDeprecated;
        }
    }
}
