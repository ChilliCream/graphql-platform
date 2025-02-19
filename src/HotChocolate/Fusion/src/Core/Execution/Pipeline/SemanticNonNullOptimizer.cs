using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class SemanticNonNullOptimizer : ISelectionSetOptimizer
{
    // TODO: Pull this out to somewhere
    private const int MaxLevels = 3;

    private static readonly Selection.CustomOptionsFlags[] _levelOptions =
    [
        Selection.CustomOptionsFlags.Option5,
        Selection.CustomOptionsFlags.Option6,
        Selection.CustomOptionsFlags.Option7
    ];

    public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
    {
        foreach (var selection in context.Selections.Values)
        {
            var semanticNonNullDirective = selection.Field.Directives
                .FirstOrDefault(d => d.Type.Name == WellKnownDirectives.SemanticNonNull);

            if (semanticNonNullDirective is null)
            {
                continue;
            }

            var levels = semanticNonNullDirective.GetArgumentValue<List<int>>(WellKnownDirectives.Levels).Order();

            var levelOption = Selection.CustomOptionsFlags.None;

            foreach (var level in levels)
            {
                // We only have 8 flags available. 3 are already taken and we want to allow for potential other
                // flags later, so we're only using the last 3 flags.
                if (level >= MaxLevels)
                {
                    continue;
                }

                levelOption |= _levelOptions[level];
            }

            selection.SetOption(levelOption);
        }
    }
}
