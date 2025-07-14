using System.Buffers;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using GreenDonut.Data.Internal;
using static GreenDonut.Data.GreenDonutPredicateDataLoaderExtensions;
using static GreenDonut.Data.Internal.DataLoaderStateHelper;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Provides extension methods to pass a pagination context to a DataLoader.
/// </summary>
public static class GreenDonutPaginationBatchingDataLoaderExtensions
{
    /// <summary>
    /// Branches a DataLoader with the provided <see cref="PagingArguments"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader that shall be branched.
    /// </param>
    /// <param name="pagingArguments">
    /// The paging arguments that shall exist as state in the branched DataLoader.
    /// </param>
    /// <param name="context">
    /// The query context that shall exist as state in the branched DataLoader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns a branched DataLoader with the provided <see cref="PagingArguments"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, Page<TValue>> With<TKey, TValue>(this IDataLoader<TKey, Page<TValue>> dataLoader,
        PagingArguments pagingArguments,
        QueryContext<TValue>? context = null)
        where TKey : notnull
        => WithInternal(dataLoader, pagingArguments, context);

    private static IDataLoader<TKey, Page<TValue>> WithInternal<TKey, TValue>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        PagingArguments pagingArguments,
        QueryContext<TValue>? context)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        var branchKey = pagingArguments.ComputeHash(context);
        var state = new PagingState<TValue>(pagingArguments, context);
        return (IQueryDataLoader<TKey, Page<TValue>>)dataLoader.Branch(branchKey, CreateBranch, state);
    }

    /// <summary>
    /// Adds a projection as state to the DataLoader.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader.
    /// </param>
    /// <param name="selector">
    /// The projection that shall be added as state to the DataLoader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the DataLoader with the added projection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, Page<TValue>> Select<TKey, TValue>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        Expression<Func<TValue, TValue>>? selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (selector is null)
        {
            return dataLoader;
        }

        if (dataLoader.ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value))
        {
            var context = (DefaultSelectorBuilder)value!;
            context.Add(selector);
            return dataLoader;
        }

        var branchKey = selector.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Selector, new DefaultSelectorBuilder(selector));
        return (IQueryDataLoader<TKey, Page<TValue>>)dataLoader.Branch(branchKey, CreateBranch,
            state);
    }

    /// <summary>
    /// Adds a predicate as state to the DataLoader.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader.
    /// </param>
    /// <param name="predicate">
    /// The predicate that shall be added as state to the DataLoader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the DataLoader with the added projection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, Page<TValue>> Where<TKey, TValue>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        Expression<Func<TValue, bool>>? predicate)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (predicate is null)
        {
            return dataLoader;
        }

        var branchKey = predicate.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Predicate,
            GetOrCreateBuilder(dataLoader.ContextData, predicate));
        return (IQueryDataLoader<TKey, Page<TValue>>)dataLoader.Branch(branchKey, CreateBranch,
            state);
    }

    /// <summary>
    /// Adds a sorting definition as state to the DataLoader.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader.
    /// </param>
    /// <param name="sortDefinition">
    /// The sorting definition that shall be added as state to the DataLoader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the DataLoader with the added projection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, Page<TValue>> OrderBy<TKey, TValue>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        SortDefinition<TValue>? sortDefinition)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (sortDefinition is null)
        {
            return dataLoader;
        }

        var branchKey = sortDefinition.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Sorting, sortDefinition);
        return (IQueryDataLoader<TKey, Page<TValue>>)dataLoader.Branch(branchKey, CreateBranch,
            state);
    }

    private static string ComputeHash<T>(this PagingArguments arguments, QueryContext<T>? context)
    {
        var hasher = ExpressionHasherPool.Shared.Get();

        hasher.Add(arguments);

        if (context is not null)
        {
            hasher.Add(context);
        }

        var s = hasher.Compute();
        ExpressionHasherPool.Shared.Return(hasher);
        return s;
    }

    private static void Add(
        this ExpressionHasher hasher,
        PagingArguments pagingArguments)
    {
        var requiredBufferSize = 1;

        requiredBufferSize += EstimateIntLength(pagingArguments.First);
        if (pagingArguments.After is not null)
        {
            requiredBufferSize += pagingArguments.After?.Length ?? 0;
            requiredBufferSize += 2;
        }

        requiredBufferSize += EstimateIntLength(pagingArguments.Last);

        if (pagingArguments.Before is not null)
        {
            requiredBufferSize += pagingArguments.Before?.Length ?? 0;
            requiredBufferSize += 2;
        }

        if (requiredBufferSize == 1)
        {
            hasher.Add('-');
            return;
        }

        char[]? rentedBuffer = null;
        var buffer = requiredBufferSize <= 128
            ? stackalloc char[requiredBufferSize]
            : (rentedBuffer = ArrayPool<char>.Shared.Rent(requiredBufferSize));

        var written = 1;
        buffer[0] = '-';

        if (pagingArguments.First.HasValue)
        {
            var span = buffer[written..];
            span[0] = 'f';
            span[1] = ':';
            written += 2;

            if (!pagingArguments.First.Value.TryFormat(buffer[written..], out var charsWritten))
            {
                throw new InvalidOperationException("Buffer is too small.");
            }

            written += charsWritten;
        }

        if (pagingArguments.After is not null)
        {
            var span = buffer[written..];
            span[0] = 'a';
            span[1] = ':';
            written += 2;

            var after = pagingArguments.After.AsSpan();
            after.CopyTo(buffer[written..]);
            written += after.Length;
        }

        if (pagingArguments.Last.HasValue)
        {
            var span = buffer[written..];
            span[0] = 'l';
            span[1] = ':';
            written += 2;

            if (!pagingArguments.Last.Value.TryFormat(buffer[written..], out var charsWritten))
            {
                throw new InvalidOperationException("Buffer is too small.");
            }

            written += charsWritten;
        }

        if (pagingArguments.Before is not null)
        {
            var span = buffer[written..];
            span[0] = 'b';
            span[1] = ':';
            written += 2;

            var before = pagingArguments.Before.AsSpan();
            before.CopyTo(buffer[written..]);
            written += before.Length;
        }

        hasher.Add(buffer[..written]);

        if (rentedBuffer != null)
        {
            ArrayPool<char>.Shared.Return(rentedBuffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EstimateIntLength(int? value)
    {
        // if the value is null we need 0 digits.
        if (value is null)
        {
            return 0;
        }

        if (value == 0)
        {
            // to print 0 we need still 1 digit
            return 3;
        }

        // if the number is negative we need one more digit for the sign
        var length = value < 0 ? 1 : 0;

        // we add the number of digits the number has to the length of the number.
        length += (int)Math.Floor(Math.Log10(Math.Abs(value.Value)) + 1);

        return length + 2;
    }
}
