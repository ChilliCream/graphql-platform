using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Pipeline;

internal sealed class FieldFlagsOptimizer : ISelectionSetOptimizer
{
    private readonly FusionGraphConfiguration _config;

    public FieldFlagsOptimizer(FusionGraphConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
    {
        var typeInfo = _config.GetType<ObjectType>(context.Type.Name);

        foreach (var selection in context.Selections.Values)
        {
            if (typeInfo.Fields.TryGetValue(selection.Field.Name, out var fieldInfo))
            {
                selection.SetOption((Selection.CustomOptionsFlags)fieldInfo.Flags);
            }
        }
    }
}
