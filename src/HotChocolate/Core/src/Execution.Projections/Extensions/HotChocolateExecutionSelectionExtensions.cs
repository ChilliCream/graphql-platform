using System.Buffers;
using System.Linq.Expressions;
using GreenDonut.Data;
using HotChocolate.Execution.Projections;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution.Processing;

/// <summary>
/// Provides extension methods to work with selections.
/// </summary>
public static class HotChocolateExecutionSelectionExtensions
{
    // Treats every conditional selection as included. This is safe because include
    // conditions are capped at 64 bits, so ulong.MaxValue satisfies every condition bit.
    private const ulong IncludeAllFlags = ulong.MaxValue;
    private static readonly SelectionExpressionBuilder s_builder = new();

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
    /// Creates a selector expression from a GraphQL selection and projects exactly
    /// the fields included by the runtime @skip/@include flags.
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
        => AsSelector<TValue>(selection, IncludeAllFlags);

    public static Expression<Func<TValue, TValue>> AsSelector<TValue>(
        this Selection selection,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var selectorExpression = GetOrCreateSelectorExpression<TValue>(selection);
        var conditionMask = selectorExpression.ConditionMask;
        var maskedFlags = includeFlags & conditionMask;

        // The selector cached on the selection includes all conditional fields.
        // We can reuse it when all conditions used by this selector are included.
        if (maskedFlags == conditionMask)
        {
            return selectorExpression.Expression;
        }

        var operation = selection.DeclaringOperation;
        var cache = operation.Features.GetOrSetSafe(
            static o => o.Schema.Services.GetRequiredService<ProjectionSelectorCache>(),
            operation);

        selectorExpression = cache.GetOrCreate(
            selection,
            maskedFlags,
            static (selection, includeFlags) => CreateSelectorExpression<TValue>(selection, includeFlags));

        return selectorExpression.Expression;
    }

    private static SelectorExpression<TValue> GetOrCreateSelectorExpression<TValue>(
        Selection selection)
        => selection.Features.GetOrSetSafe(
            static selection => CreateSelectorExpression<TValue>(selection),
            selection);

    private static SelectorExpression<TValue> CreateSelectorExpression<TValue>(
        Selection selection)
        => CreateSelectorExpression<TValue>(selection, IncludeAllFlags);

    private static SelectorExpression<TValue> CreateSelectorExpression<TValue>(
        Selection selection,
        ulong includeFlags)
    {
        var flags = selection.Field.Flags;

        if ((flags & CoreFieldFlags.Connection) == CoreFieldFlags.Connection)
        {
            return CreateCompositeSelectorExpression<TValue>(
                selection,
                includeFlags,
                GetConnectionSelections);
        }

        if ((flags & CoreFieldFlags.CollectionSegment) == CoreFieldFlags.CollectionSegment)
        {
            return CreateCompositeSelectorExpression<TValue>(
                selection,
                includeFlags,
                GetCollectionSelections);
        }

        Expression<Func<TValue, TValue>> expression;
        ulong conditionMask;

        if ((flags & CoreFieldFlags.GlobalIdNodeField) == CoreFieldFlags.GlobalIdNodeField
            || (flags & CoreFieldFlags.GlobalIdNodesField) == CoreFieldFlags.GlobalIdNodesField)
        {
            expression = s_builder.BuildNodeExpression<TValue>(selection, includeFlags, out conditionMask);
        }
        else
        {
            expression = s_builder.BuildExpression<TValue>(selection, includeFlags, out conditionMask);
        }

        return new SelectorExpression<TValue>(includeFlags, conditionMask, expression);
    }

    private static SelectorExpression<TValue> CreateCompositeSelectorExpression<TValue>(
        Selection selection,
        ulong includeFlags,
        SelectionCollector collectSelections)
    {
        var builder = new DefaultSelectorBuilder();
        var conditionMask = 0UL;
        var buffer = ArrayPool<Selection>.Shared.Rent(16);

        try
        {
            var count = collectSelections(selection, buffer);
            for (var i = 0; i < count; i++)
            {
                var child = buffer[i];
                var childSelectorExpression = GetOrCreateSelectorExpression<TValue>(child);
                conditionMask |= childSelectorExpression.ConditionMask;

                var childFlags = includeFlags & childSelectorExpression.ConditionMask;
                var childExpression = childFlags == childSelectorExpression.ConditionMask
                    ? childSelectorExpression
                    : CreateSelectorExpression<TValue>(child, childFlags);

                builder.Add(childExpression.Expression);
            }
        }
        finally
        {
            ArrayPool<Selection>.Shared.Return(buffer);
        }

        return new SelectorExpression<TValue>(
            includeFlags,
            conditionMask,
            builder.TryCompile<TValue>() ?? CreateIdentity<TValue>());
    }

    private static Expression<Func<TValue, TValue>> CreateIdentity<TValue>()
    {
        var parameter = Expression.Parameter(typeof(TValue), "root");
        return Expression.Lambda<Func<TValue, TValue>>(parameter, parameter);
    }

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

    private delegate int SelectionCollector(Selection selection, Span<Selection> buffer);
}
