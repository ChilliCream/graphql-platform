using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using GreenDonut.Data;
using HotChocolate.Execution.Projections;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution.Processing;

/// <summary>
/// Provides extension methods to work with selections.
/// </summary>
public static class HotChocolateExecutionSelectionExtensions
{
    /// <summary>
    /// Creates a selector expression from a GraphQL selection.
    /// </summary>
    /// <param name="selection">
    /// The selection that shall be converted into a selector expression.
    /// </param>
    /// <typeparam name="TValue">
    /// The type of the value that is returned by the <see cref="ISelection"/>.
    /// </typeparam>
    /// <returns>
    /// Returns a selector expression that can be used for data projections.
    /// </returns>
    public static Expression<Func<TValue, TValue>> AsSelector<TValue>(
        this ISelection selection)
    {
        if (selection is not Selection casted)
        {
            throw new ArgumentException(
                $"Expected {typeof(Selection).FullName!}.",
                nameof(selection));
        }

        return AsSelector<TValue>(casted);
    }

    /// <summary>
    /// Creates a selector expression from a GraphQL selection and applies
    /// runtime include/skip directive flags.
    /// </summary>
    public static Expression<Func<TValue, TValue>> AsSelector<TValue>(
        this ISelection selection,
        ulong includeFlags)
    {
        if (selection is not Selection casted)
        {
            throw new ArgumentException(
                $"Expected {typeof(Selection).FullName!}.",
                nameof(selection));
        }

        return AsSelector<TValue>(casted, includeFlags);
    }

    /// <summary>
    /// Creates a selector expression from a GraphQL selection.
    /// </summary>
    /// <param name="selection">
    /// The selection that shall be converted into a selector expression.
    /// </param>
    /// <typeparam name="TValue">
    /// The type of the value that is returned by the <see cref="ISelection"/>.
    /// </typeparam>
    /// <returns>
    /// Returns a selector expression that can be used for data projections.
    /// </returns>
    public static Expression<Func<TValue, TValue>> AsSelector<TValue>(
        this Selection selection)
        => AsSelector<TValue>(selection, 0);

    public static Expression<Func<TValue, TValue>> AsSelector<TValue>(
        this Selection selection,
        ulong includeFlags)
    {
        // we first check if we already have an expression for this selection,
        // this would be the cheapest way to get the expression. A cached expression is
        // only ever stored when the projected selection set (at any depth) contained no
        // conditional (@include/@skip) selection, so it is independent of the runtime
        // include flags and always safe to reuse here.
        if (TryGetExpression<TValue>(selection, out var expression))
        {
            return expression;
        }

        // we do not know up front whether the projection depends on the runtime include
        // flags (a conditional selection can sit arbitrarily deep in the selection set),
        // so we build the expression while tracking whether any conditional selection
        // participated and only cache the result when none did.
        var tracker = new SelectionExpressionBuilder.ConditionalTracker();

        // we first check what kind of field selection we have,
        // connection, collection or single field.
        var flags = selection.Field.Flags;

        if ((flags & CoreFieldFlags.Connection) == CoreFieldFlags.Connection)
        {
            var builder = new DefaultSelectorBuilder();
            var buffer = ArrayPool<Selection>.Shared.Rent(16);
            var count = GetConnectionSelections(selection, buffer);
            for (var i = 0; i < count; i++)
            {
                builder.Add(buffer[i].BuildExpression<TValue>(includeFlags, tracker));
            }
            ArrayPool<Selection>.Shared.Return(buffer);
            return selection.CacheIfStable(builder.TryCompile<TValue>()!, tracker);
        }

        if ((flags & CoreFieldFlags.CollectionSegment) == CoreFieldFlags.CollectionSegment)
        {
            var builder = new DefaultSelectorBuilder();
            var buffer = ArrayPool<Selection>.Shared.Rent(16);
            var count = GetCollectionSelections(selection, buffer);
            for (var i = 0; i < count; i++)
            {
                builder.Add(buffer[i].BuildExpression<TValue>(includeFlags, tracker));
            }
            ArrayPool<Selection>.Shared.Return(buffer);
            return selection.CacheIfStable(builder.TryCompile<TValue>()!, tracker);
        }

        if ((flags & CoreFieldFlags.GlobalIdNodeField) == CoreFieldFlags.GlobalIdNodeField
            || (flags & CoreFieldFlags.GlobalIdNodesField) == CoreFieldFlags.GlobalIdNodesField)
        {
            return selection.CacheIfStable(selection.BuildNodeExpression<TValue>(includeFlags, tracker), tracker);
        }

        return selection.CacheIfStable(selection.BuildExpression<TValue>(includeFlags, tracker), tracker);
    }

    private static bool TryGetExpression<TValue>(
        Selection selection,
        [NotNullWhen(true)] out Expression<Func<TValue, TValue>>? expression)
        => selection.Features.TryGet(out expression);

    private static int GetConnectionSelections(Selection selection, Span<Selection> buffer)
    {
        var pageType = (ObjectType)selection.Field.Type.NamedType();
        var connectionSelections = selection.DeclaringOperation.GetSelectionSet(selection, pageType);
        var count = 0;

        foreach (var connectionChild in connectionSelections.Selections)
        {
            if (connectionChild.Field.Name.EqualsOrdinal("nodes"))
            {
                if (buffer.Length == count)
                {
                    throw new InvalidOperationException("To many alias selections of nodes and edges.");
                }

                buffer[count++] = connectionChild;
            }
            else if (connectionChild.Field.Name.EqualsOrdinal("edges"))
            {
                var edgeType = (ObjectType)connectionChild.Field.Type.NamedType();
                var edgeSelections = connectionChild.DeclaringOperation.GetSelectionSet(connectionChild, edgeType);

                foreach (var edgeChild in edgeSelections.Selections)
                {
                    if (edgeChild.Field.Name.EqualsOrdinal("node"))
                    {
                        if (buffer.Length == count)
                        {
                            throw new InvalidOperationException("To many alias selections of nodes and edges.");
                        }

                        buffer[count++] = edgeChild;
                    }
                }
            }
        }

        return count;
    }

    private static int GetCollectionSelections(Selection selection, Span<Selection> buffer)
    {
        var pageType = (ObjectType)selection.Field.Type.NamedType();
        var connectionSelections = selection.DeclaringOperation.GetSelectionSet(selection, pageType);
        var count = 0;

        foreach (var connectionChild in connectionSelections.Selections)
        {
            if (connectionChild.Field.Name.EqualsOrdinal("items"))
            {
                if (buffer.Length == count)
                {
                    throw new InvalidOperationException("To many alias selections of items.");
                }

                buffer[count++] = connectionChild;
            }
        }

        return count;
    }
}

file static class Extensions
{
    private static readonly SelectionExpressionBuilder s_builder = new();

    extension(Selection selection)
    {
        public Expression<Func<TValue, TValue>> BuildExpression<TValue>(
            ulong includeFlags,
            SelectionExpressionBuilder.ConditionalTracker tracker)
            => s_builder.BuildExpression<TValue>(selection, includeFlags, tracker);

        public Expression<Func<TValue, TValue>> BuildNodeExpression<TValue>(
            ulong includeFlags,
            SelectionExpressionBuilder.ConditionalTracker tracker)
            => s_builder.BuildNodeExpression<TValue>(selection, includeFlags, tracker);

        // Cache the built expression for reuse only when it does not depend on the runtime
        // include flags (i.e. no conditional @include/@skip selection participated). A
        // conditional projection must be rebuilt per request from the current flags.
        public Expression<Func<TValue, TValue>> CacheIfStable<TValue>(
            Expression<Func<TValue, TValue>> expression,
            SelectionExpressionBuilder.ConditionalTracker tracker)
            => tracker.IsConditional
                ? expression
                : selection.Features.GetOrSetSafe(() => expression);
    }
}
