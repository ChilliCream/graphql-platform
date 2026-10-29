using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections.Expressions;

internal static class QueryableListArgumentOptimizer
{
    public static void Optimize(
        SelectionSetOptimizerContext context,
        QueryableProjectionProvider provider)
    {
        var selections = context.Selections;

        for (var i = 0; i < selections.Length; i++)
        {
            var selection = selections[i];

            if (!selection.IsList
                || selection.Field.Member is not PropertyInfo { CanWrite: true } member
                || (!selection.HasFilterFeature() && !selection.HasSortingFeature))
            {
                continue;
            }

            for (var j = i + 1; j < selections.Length; j++)
            {
                var other = selections[j];

                if (other.Field.Member == member && !HaveSameArguments(selection, other))
                {
                    DeferArguments(context, selection, provider);
                    DeferArguments(context, other, provider);
                }
            }
        }
    }

    private static bool HaveSameArguments(Selection left, Selection right)
    {
        var leftArguments = left.SyntaxNodes[0].Node.Arguments;
        var rightArguments = right.SyntaxNodes[0].Node.Arguments;

        if (leftArguments.Count != rightArguments.Count)
        {
            return false;
        }

        foreach (var argument in leftArguments)
        {
            var found = false;

            foreach (var other in rightArguments)
            {
                if (argument.Name.Value == other.Name.Value
                    && SyntaxComparer.BySyntax.Equals(argument.Value, other.Value))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static void DeferArguments(
        SelectionSetOptimizerContext context,
        Selection selection,
        QueryableProjectionProvider provider)
    {
        var features = context.CreateSelectionFeatures(selection);

        if (features.Get<DeferredArguments>() is not null)
        {
            return;
        }

        if (provider.CreateDeferredListHandler(selection) is not { } handler)
        {
            return;
        }

        features.SetSafe(DeferredArguments.Instance);
        features.SetSafe(handler);

        var pipeline = context.CompileResolverPipeline(selection.Field, selection.SyntaxNodes[0].Node);
        context.SetResolver(
            selection,
            ctx =>
            {
                ctx.LocalContextData = ctx.LocalContextData
                    .SetItem(QueryableFilterProvider.SkipFilteringKey, false)
                    .SetItem(QueryableSortProvider.SkipSortingKey, false);
                return pipeline(ctx);
            });
    }

    private sealed class DeferredArguments
    {
        public static DeferredArguments Instance { get; } = new();
    }
}
