using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Rejects an operation whose selection sets, with its fragment spreads followed, are nested
/// deeper than the allowed depth or contain more than the allowed number of selections. Depth and
/// selection count are measured in a single walk that visits every fragment only once. Because the
/// walk follows fragment spreads, it also detects fragment cycles and stops with the standard cycle
/// error so it cannot recurse without end; the canonical check is <see cref="FragmentVisitor"/>.
/// </summary>
internal sealed class OperationSizeRule(int maxAllowedSelectionSetDepth, int maxAllowedSelections)
    : IDocumentValidatorRule
{
    private const int Measuring = -1;

    public ushort Priority => ushort.MinValue;

    public bool IsCacheable => true;

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);

        var fragmentSizes = context.Features.GetOrSet<OperationSizeFeature>().FragmentSizes;

        var definitions = document.Definitions;

        for (var i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] is OperationDefinitionNode operation
                && !TryMeasure(context, fragmentSizes, operation.SelectionSet, 0, out _, out _))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Measures how many levels of selection sets <paramref name="selectionSet"/> spans and how
    /// many selections it has, where <paramref name="parentDepth"/> is the number of levels that
    /// enclose it.
    /// </summary>
    private bool TryMeasure(
        DocumentValidatorContext context,
        Dictionary<string, (int Depth, long Count)> fragmentSizes,
        SelectionSetNode selectionSet,
        int parentDepth,
        out int depth,
        out long count)
    {
        depth = 1;
        count = selectionSet.Selections.Count;

        if (parentDepth + depth > maxAllowedSelectionSetDepth)
        {
            context.ReportMaxSelectionSetDepthOverflow(selectionSet, maxAllowedSelectionSetDepth);
            return false;
        }

        if (count > maxAllowedSelections)
        {
            context.ReportMaxSelectionCountOverflow(selectionSet, maxAllowedSelections);
            return false;
        }

        parentDepth++;

        var selections = selectionSet.Selections;

        for (var i = 0; i < selections.Count; i++)
        {
            int childDepth;
            long childCount;

            switch (selections[i])
            {
                case FieldNode { SelectionSet: { } childSelectionSet } field:
                    context.Path.Push(field);

                    if (!TryMeasure(context, fragmentSizes, childSelectionSet, parentDepth, out childDepth, out childCount))
                    {
                        return false;
                    }

                    context.Path.Pop();
                    break;

                case InlineFragmentNode { SelectionSet: var childSelectionSet }:
                    if (!TryMeasure(context, fragmentSizes, childSelectionSet, parentDepth, out childDepth, out childCount))
                    {
                        return false;
                    }
                    break;

                case FragmentSpreadNode spread:
                    if (!TryMeasure(context, fragmentSizes, spread, parentDepth, out childDepth, out childCount))
                    {
                        return false;
                    }
                    break;

                default:
                    continue;
            }

            count += childCount;

            if (count > maxAllowedSelections)
            {
                context.ReportMaxSelectionCountOverflow(selectionSet, maxAllowedSelections);
                return false;
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
        Dictionary<string, (int Depth, long Count)> fragmentSizes,
        FragmentSpreadNode spread,
        int parentDepth,
        out int depth,
        out long count)
    {
        // A fragment spans the same number of levels and has the same number of selections
        // wherever it is spread, so we measure every fragment only once.
        if (fragmentSizes.TryGetValue(spread.Name.Value, out var size))
        {
            depth = size.Depth;
            count = size.Count;

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
            depth = 0;
            count = 0;
            return true;
        }

        fragmentSizes[spread.Name.Value] = (Measuring, 0);

        if (!TryMeasure(context, fragmentSizes, fragment.SelectionSet, parentDepth, out depth, out count))
        {
            return false;
        }

        fragmentSizes[spread.Name.Value] = (depth, count);
        return true;
    }
}

file sealed class OperationSizeFeature : ValidatorFeature
{
    public Dictionary<string, (int Depth, long Count)> FragmentSizes { get; } = new(StringComparer.Ordinal);

    protected internal override void Reset() => FragmentSizes.Clear();
}
