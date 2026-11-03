using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Rejects an operation whose selection sets, with its fragment spreads followed,
/// are nested deeper than the allowed depth or whose fragment spreads form a cycle.
/// </summary>
internal sealed class SelectionSetDepthRule(int maxAllowedSelectionSetDepth) : IDocumentValidatorRule
{
    private const int Measuring = -1;

    public ushort Priority => ushort.MinValue;

    public bool IsCacheable => true;

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);

        var fragmentDepths = context.Features.GetOrSet<SelectionSetDepthFeature>().FragmentDepths;

        var definitions = document.Definitions;

        for (var i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] is OperationDefinitionNode operation
                && !TryMeasure(context, fragmentDepths, operation.SelectionSet, 0, out _))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Measures how many levels of selection sets <paramref name="selectionSet"/> spans,
    /// where <paramref name="parentDepth"/> is the number of levels that enclose it.
    /// </summary>
    private bool TryMeasure(
        DocumentValidatorContext context,
        Dictionary<string, int> fragmentDepths,
        SelectionSetNode selectionSet,
        int parentDepth,
        out int depth)
    {
        depth = 1;

        if (parentDepth + depth > maxAllowedSelectionSetDepth)
        {
            context.ReportMaxSelectionSetDepthOverflow(selectionSet, maxAllowedSelectionSetDepth);
            return false;
        }

        parentDepth++;

        var selections = selectionSet.Selections;

        for (var i = 0; i < selections.Count; i++)
        {
            int childDepth;

            switch (selections[i])
            {
                case FieldNode { SelectionSet: { } childSelectionSet } field:
                    context.Path.Push(field);

                    if (!TryMeasure(context, fragmentDepths, childSelectionSet, parentDepth, out childDepth))
                    {
                        return false;
                    }

                    context.Path.Pop();
                    break;

                case InlineFragmentNode { SelectionSet: var childSelectionSet }:
                    if (!TryMeasure(context, fragmentDepths, childSelectionSet, parentDepth, out childDepth))
                    {
                        return false;
                    }
                    break;

                case FragmentSpreadNode spread:
                    if (!TryMeasure(context, fragmentDepths, spread, parentDepth, out childDepth))
                    {
                        return false;
                    }
                    break;

                default:
                    continue;
            }

            if (depth <= childDepth)
            {
                depth = childDepth + 1;
            }
        }

        return true;
    }

    private bool TryMeasure(
        DocumentValidatorContext context,
        Dictionary<string, int> fragmentDepths,
        FragmentSpreadNode spread,
        int parentDepth,
        out int depth)
    {
        // A fragment spans the same number of levels wherever it is spread,
        // so we measure every fragment only once.
        if (fragmentDepths.TryGetValue(spread.Name.Value, out depth))
        {
            if (depth is Measuring)
            {
                context.FatalErrorDetected = true;
                context.ReportError(context.FragmentCycleDetected(spread));
                return false;
            }

            if (parentDepth + depth > maxAllowedSelectionSetDepth)
            {
                context.ReportMaxSelectionSetDepthOverflow(spread, maxAllowedSelectionSetDepth);
                return false;
            }

            return true;
        }

        if (!context.Fragments.TryGet(spread, out var fragment))
        {
            return true;
        }

        fragmentDepths[spread.Name.Value] = Measuring;

        if (!TryMeasure(context, fragmentDepths, fragment.SelectionSet, parentDepth, out depth))
        {
            return false;
        }

        fragmentDepths[spread.Name.Value] = depth;
        return true;
    }
}

file sealed class SelectionSetDepthFeature : ValidatorFeature
{
    public Dictionary<string, int> FragmentDepths { get; } = new(StringComparer.Ordinal);

    protected internal override void Reset() => FragmentDepths.Clear();
}
