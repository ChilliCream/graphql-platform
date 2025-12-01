// ReSharper disable InconsistentlySynchronizedField
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using GreenDonut.Data;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Projections;

internal sealed class ExpressionCache
{
    private readonly object _writeLock = new();
    private readonly ConcurrentDictionary<int, Expression> _cache = new();

    public bool TryGetExpression<TValue>(
        Selection selection,
        [NotNullWhen(true)] out Expression<Func<TValue, TValue>>? expression)
    {
        if (_cache.TryGetValue(selection.Id, out var cachedExpression)
            && cachedExpression is Expression<Func<TValue, TValue>> casted)
        {
            expression = casted;
            return true;
        }

        expression = null;
        return false;
    }

    public Expression<Func<TValue, TValue>> GetOrCreateExpression<TValue>(
        Selection selection,
        SelectionExpressionBuilder  expressionBuilder)
    {
        if (!TryGetExpression<TValue>(selection, out var expression))
        {
            lock (_writeLock)
            {
                if (!TryGetExpression(selection, out expression))
                {
                    expression = expressionBuilder.BuildExpression<TValue>(selection);
                    _cache.TryAdd(selection.Id, expression);
                }
            }
        }

        return expression;
    }

    public Expression<Func<TValue, TValue>> GetOrCreateExpression<TValue>(
        Selection selection,
        ISelectorBuilder expressionBuilder)
    {
        if (!TryGetExpression<TValue>(selection, out var expression))
        {
            lock (_writeLock)
            {
                if (!TryGetExpression(selection, out expression))
                {
                    expression = expressionBuilder.TryCompile<TValue>()!;
                    _cache.TryAdd(selection.Id, expression);
                }
            }
        }

        return expression;
    }

    public Expression<Func<TValue, TValue>> GetOrCreateNodeExpression<TValue>(
        Selection selection,
        SelectionExpressionBuilder  expressionBuilder)
    {
        if (!TryGetExpression<TValue>(selection, out var expression))
        {
            lock (_writeLock)
            {
                if (!TryGetExpression(selection, out expression))
                {
                    expression = expressionBuilder.BuildNodeExpression<TValue>(selection);
                    _cache.TryAdd(selection.Id, expression);
                }
            }
        }

        return expression;
    }
}
