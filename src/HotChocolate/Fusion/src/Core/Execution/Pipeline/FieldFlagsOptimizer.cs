using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class FieldFlagsOptimizer(FusionGraphConfiguration config) : ISelectionSetOptimizer
{
    private readonly FusionGraphConfiguration _config = config
        ?? throw new ArgumentNullException(nameof(config));

    public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
    {
        if (!_config.TryGetType<ObjectTypeMetadata>(context.Type.Name, out var typeInfo))
        {
            return;
        }

        foreach (var selection in context.Selections.Values)
        {
            if (typeInfo.Fields.TryGetField(selection.Field.Name, out var fieldInfo))
            {
                selection.SetOption((Selection.CustomOptionsFlags)fieldInfo.Flags);
            }
        }
    }
}
