using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal static class InputObjectMergeExtensions
{
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
