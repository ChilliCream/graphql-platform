using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Rejects an operation whose selection sets, with its fragment spreads followed,
/// are nested deeper than the allowed depth or whose fragment spreads form a cycle.
/// </summary>
internal sealed class SelectionSetDepthRule(ValidationOptions options) : IDocumentValidatorRule
{
    private const int Measuring = -1;

    public bool IsCacheable => true;

    public void Validate(IDocumentValidatorContext context, DocumentNode document)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var maxAllowedSelectionSetDepth = options.MaxAllowedSelectionSetDepth;
        var fragmentDepths = new Dictionary<string, int>(StringComparer.Ordinal);

        var definitions = document.Definitions;

        for (var i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] is OperationDefinitionNode operation
                && !TryMeasure(
                    context,
                    fragmentDepths,
                    maxAllowedSelectionSetDepth,
                    operation.SelectionSet,
                    0,
                    out _))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Measures how many levels of selection sets <paramref name="selectionSet"/> spans,
    /// where <paramref name="parentDepth"/> is the number of levels that enclose it.
    /// </summary>
    private static bool TryMeasure(
        IDocumentValidatorContext context,
        Dictionary<string, int> fragmentDepths,
        int maxAllowedSelectionSetDepth,
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

                    if (!TryMeasure(
                        context,
                        fragmentDepths,
                        maxAllowedSelectionSetDepth,
                        childSelectionSet,
                        parentDepth,
                        out childDepth))
                    {
                        return false;
                    }

                    context.Path.Pop();
                    break;

                case InlineFragmentNode { SelectionSet: var childSelectionSet }:
                    if (!TryMeasure(
                        context,
                        fragmentDepths,
                        maxAllowedSelectionSetDepth,
                        childSelectionSet,
                        parentDepth,
                        out childDepth))
                    {
                        return false;
                    }
                    break;

                case FragmentSpreadNode spread:
                    if (!TryMeasure(
                        context,
                        fragmentDepths,
                        maxAllowedSelectionSetDepth,
                        spread,
                        parentDepth,
                        out childDepth))
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

    private static bool TryMeasure(
        IDocumentValidatorContext context,
        Dictionary<string, int> fragmentDepths,
        int maxAllowedSelectionSetDepth,
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

        if (!context.Fragments.TryGetValue(spread.Name.Value, out var fragment))
        {
            return true;
        }

        fragmentDepths[spread.Name.Value] = Measuring;

        if (!TryMeasure(
            context,
            fragmentDepths,
            maxAllowedSelectionSetDepth,
            fragment.SelectionSet,
            parentDepth,
            out depth))
        {
            return false;
        }

        fragmentDepths[spread.Name.Value] = depth;

        return true;
    }
}
