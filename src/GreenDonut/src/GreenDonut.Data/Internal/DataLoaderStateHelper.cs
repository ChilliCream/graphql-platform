using System.Buffers;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace GreenDonut.Data.Internal;

internal static class DataLoaderStateHelper
{
    internal static IDataLoader CreateBranch<TKey, TValue>(
        string branchKey,
        IDataLoader<TKey, TValue> dataLoader,
        QueryState state)
        where TKey : notnull
    {
        var branch = new QueryDataLoader<TKey, TValue>(
            (DataLoaderBase<TKey, TValue>)dataLoader,
            branchKey);
        branch.SetState(state.Key, state.Value);
        return branch;
    }

    internal static IDataLoader CreateBranch<TKey, TValue, TElement>(
        string branchKey,
        IDataLoader<TKey, TValue> dataLoader,
        QueryContext<TElement> state)
        where TKey : notnull
    {
        var branch = new QueryDataLoader<TKey, TValue>(
            (DataLoaderBase<TKey, TValue>)dataLoader,
            branchKey);

        if (state.Selector is not null)
        {
            branch.SetState(DataLoaderStateKeys.Selector, new DefaultSelectorBuilder(state.Selector));
        }

        if (state.Predicate is not null)
        {
            branch.SetState(DataLoaderStateKeys.Predicate, new DefaultPredicateBuilder(state.Predicate));
        }

        if (state.Sorting is not null)
        {
            branch.SetState(DataLoaderStateKeys.Sorting, state.Sorting);
        }

        return branch;
    }

    internal static IDataLoader CreateBranch<TKey, TValue, TElement>(
        string branchKey,
        IDataLoader<TKey, Page<TElement>> dataLoader,
        PagingState<TValue> state)
        where TKey : notnull
    {
        var branch = new QueryDataLoader<TKey, Page<TValue>>(
            (DataLoaderBase<TKey, Page<TValue>>)dataLoader,
            branchKey);

        ApplyPagingState(branch, state);

        return branch;
    }

    internal static IDataLoader CreateBranch<TKey, TValue, TElement>(
        string branchKey,
        IDataLoader<TKey, StreamPage<TElement>> dataLoader,
        PagingState<TValue> state)
        where TKey : notnull
    {
        var branch = new QueryDataLoader<TKey, StreamPage<TValue>>(
            (DataLoaderBase<TKey, StreamPage<TValue>>)dataLoader,
            branchKey);

        ApplyPagingState(branch, state);

        return branch;
    }

    private static void ApplyPagingState<TKey, TBranchValue, TValue>(
        QueryDataLoader<TKey, TBranchValue> branch,
        PagingState<TValue> state)
        where TKey : notnull
    {
        branch.SetState(DataLoaderStateKeys.PagingArgs, state.PagingArgs);

        if (state.Context is not null)
        {
            if (state.Context.Selector is not null)
            {
                branch.SetState(DataLoaderStateKeys.Selector, new DefaultSelectorBuilder(state.Context.Selector));
            }

            if (state.Context.Predicate is not null)
            {
                branch.SetState(DataLoaderStateKeys.Predicate, new DefaultPredicateBuilder(state.Context.Predicate));
            }

            if (state.Context.Sorting is not null)
            {
                branch.SetState(DataLoaderStateKeys.Sorting, state.Context.Sorting);
            }
        }
    }

    internal static string ComputeHash<T>(this PagingArguments arguments, QueryContext<T>? context)
    {
        var hasher = ExpressionHasherPool.Shared.Get();

        hasher.Add(arguments);

        if (context is not null)
        {
            hasher.Add(context);
        }

        var hash = hasher.Compute();
        ExpressionHasherPool.Shared.Return(hasher);
        return hash;
    }

    /// <summary>
    /// Appends the paging arguments to the branch key. Values equal to their defaults add no
    /// bytes, and each non-default value appends its own marker.
    /// </summary>
    internal static ExpressionHasher Add(this ExpressionHasher hasher, PagingArguments pagingArguments)
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

        // Only reserve space for the marker when items are excluded, so the hash of every
        // existing argument combination (IncludeItems true) stays byte-identical.
        if (!pagingArguments.IncludeItems)
        {
            requiredBufferSize += 2;
        }

        if (pagingArguments.IncludeTotalCount)
        {
            requiredBufferSize += 2;
        }

        if (pagingArguments.EnableRelativeCursors)
        {
            requiredBufferSize += 2;
        }

        if (pagingArguments.NullOrdering != NullOrdering.Unspecified)
        {
            requiredBufferSize += EstimateIntLength((int)pagingArguments.NullOrdering);
        }

        if (requiredBufferSize == 1)
        {
            hasher.Add('-');
            return hasher;
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

            if (!pagingArguments.First.Value.TryFormat(
                buffer[written..],
                out var charsWritten,
                provider: CultureInfo.InvariantCulture))
            {
                throw ThrowHelper.PagingArgumentsHash_BufferTooSmall();
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

            if (!pagingArguments.Last.Value.TryFormat(
                buffer[written..],
                out var charsWritten,
                provider: CultureInfo.InvariantCulture))
            {
                throw ThrowHelper.PagingArgumentsHash_BufferTooSmall();
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

        if (!pagingArguments.IncludeItems)
        {
            var span = buffer[written..];
            span[0] = 'n';
            span[1] = ':';
            written += 2;
        }

        if (pagingArguments.IncludeTotalCount)
        {
            var span = buffer[written..];
            span[0] = 't';
            span[1] = ':';
            written += 2;
        }

        if (pagingArguments.EnableRelativeCursors)
        {
            var span = buffer[written..];
            span[0] = 'r';
            span[1] = ':';
            written += 2;
        }

        if (pagingArguments.NullOrdering != NullOrdering.Unspecified)
        {
            var span = buffer[written..];
            span[0] = 'o';
            span[1] = ':';
            written += 2;

            if (!((int)pagingArguments.NullOrdering).TryFormat(
                buffer[written..],
                out var charsWritten,
                provider: CultureInfo.InvariantCulture))
            {
                throw ThrowHelper.PagingArgumentsHash_BufferTooSmall();
            }

            written += charsWritten;
        }

        hasher.Add(buffer[..written]);

        if (rentedBuffer != null)
        {
            ArrayPool<char>.Shared.Return(rentedBuffer);
        }

        return hasher;
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

    internal static string ComputeHash<TValue>(this QueryContext<TValue> state)
    {
        var hasher = ExpressionHasherPool.Shared.Get();

        if (state.Selector is not null)
        {
            hasher.Add(state.Selector);
        }

        if (state.Predicate is not null)
        {
            hasher.Add(state.Predicate);
        }

        if (state.Sorting is not null)
        {
            hasher.Add(state.Sorting);
        }

        var hash = hasher.Compute();
        ExpressionHasherPool.Shared.Return(hasher);
        return hash;
    }

    internal static ExpressionHasher Add<TValue>(this ExpressionHasher hasher, QueryContext<TValue> state)
    {
        if (state.Selector is not null)
        {
            hasher.Add(state.Selector);
        }

        if (state.Predicate is not null)
        {
            hasher.Add(state.Predicate);
        }

        if (state.Sorting is not null)
        {
            hasher.Add(state.Sorting);
        }

        return hasher;
    }

    public static string ComputeHash(this Expression expression)
    {
        var hasher = ExpressionHasherPool.Shared.Get();
        var branchKey = hasher.Add(expression).Compute();
        ExpressionHasherPool.Shared.Return(hasher);
        return branchKey;
    }

    public static string ComputeHash<T>(this SortDefinition<T> sortDefinition)
    {
        var hasher = ExpressionHasherPool.Shared.Get();
        var branchKey = hasher.Add(sortDefinition).Compute();
        ExpressionHasherPool.Shared.Return(hasher);
        return branchKey;
    }
}
