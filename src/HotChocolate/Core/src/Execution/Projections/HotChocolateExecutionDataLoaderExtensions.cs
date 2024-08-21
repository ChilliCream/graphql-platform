#if NET8_0_OR_GREATER
#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Projections;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Projections;

[Experimental(Experiments.Projections)]
public static class HotChocolateExecutionDataLoaderExtensions
{
    private static readonly SelectionExpressionBuilder _builder = new();

    public static ISelectionDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        ISelection selection)
        where TKey : notnull
    {
        var key = $"{dataLoader.GetType().FullName!}.{selection.Id}";
        var expression = selection.DeclaringOperation
            .GetOrAddState<Expression<Func<TValue, TValue>>, SelectionExpressionBuilder>(
                key,
                (_, b) => b.BuildExpression<TValue>(selection),
                _builder);
        return dataLoader.Select(expression);
    }
}
#endif
