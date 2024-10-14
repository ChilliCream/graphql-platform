using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using HotChocolate.Pagination;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Selectors;

/// <summary>
/// Provides extension methods to pass a pagination context to a DataLoader.
/// </summary>
public static class HotChocolatePaginationBatchingDataLoaderSelectorExtensions
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
    public static IPagingDataLoader<TKey, Page<TValue>> WithPagingArguments<TKey, TValue>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        PagingArguments pagingArguments)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        var branchKey = CreateBranchKey(pagingArguments);
        return (IPagingDataLoader<TKey, Page<TValue>>)dataLoader.Branch(
            branchKey,
            CreatePagingDataLoader,
            pagingArguments);

        static IDataLoader CreatePagingDataLoader(
            string branchKey,
            IDataLoader<TKey, Page<TValue>> root,
            PagingArguments pagingArguments)
        {
            var branch = new PagingDataLoader<TKey, Page<TValue>>(root, branchKey);
            branch.SetState(pagingArguments);
            return branch;
        }
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
    /// <typeparam name="TElement">
    /// The element type of the projection.
    /// </typeparam>
    /// <returns>
    /// Returns the DataLoader with the added projection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    [Experimental(Experiments.Selectors)]
    public static IPagingDataLoader<TKey, Page<TValue>> Select<TElement, TKey, TValue>(
        this IPagingDataLoader<TKey, Page<TValue>> dataLoader,
        Expression<Func<TElement, TElement>>? selector)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            return dataLoader;
        }

        if (dataLoader.ContextData.TryGetValue(typeof(ISelectorBuilder).FullName!, out var value))
        {
            var context = (DefaultSelectorBuilder)value!;
            context.Add(selector);
            return dataLoader;
        }

        var branchKey = selector.ToString();
        return (IPagingDataLoader<TKey, Page<TValue>>)dataLoader.Branch(branchKey, CreateBranch, selector);

        static IDataLoader CreateBranch(
            string key,
            IDataLoader<TKey, Page<TValue>> dataLoader,
            Expression<Func<TElement, TElement>> selector)
        {
            var branch = new PagingDataLoader<TKey, Page<TValue>>(dataLoader, key);
            var context = new DefaultSelectorBuilder();
            branch.ContextData = branch.ContextData.SetItem(typeof(ISelectorBuilder).FullName!, context);
            context.Add(selector);
            return branch;
        }
    }

    private static string CreateBranchKey(
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
            return "-";
        }

        char[]? rentedBuffer = null;
        var buffer = requiredBufferSize <= 128
            ? stackalloc char[requiredBufferSize]
            : (rentedBuffer = ArrayPool<char>.Shared.Rent(requiredBufferSize));

        var written = 1;
        buffer[0] = '-';

        if (pagingArguments.First.HasValue)
        {
            var span = buffer.Slice(written);
            span[0] = 'f';
            span[1] = ':';
            written += 2;

            if (!pagingArguments.First.Value.TryFormat(buffer.Slice(written), out var charsWritten))
            {
                throw new InvalidOperationException("Buffer is too small.");
            }

            written += charsWritten;
        }

        if (pagingArguments.After is not null)
        {
            var span = buffer.Slice(written);
            span[0] = 'a';
            span[1] = ':';
            written += 2;

            var after = pagingArguments.After.AsSpan();
            after.CopyTo(buffer.Slice(written));
            written += after.Length;
        }

        if (pagingArguments.Last.HasValue)
        {
            var span = buffer.Slice(written);
            span[0] = 'l';
            span[1] = ':';
            written += 2;

            if (!pagingArguments.Last.Value.TryFormat(buffer.Slice(written), out var charsWritten))
            {
                throw new InvalidOperationException("Buffer is too small.");
            }

            written += charsWritten;
        }

        if (pagingArguments.Before is not null)
        {
            var span = buffer.Slice(written);
            span[0] = 'b';
            span[1] = ':';
            written += 2;

            var before = pagingArguments.Before.AsSpan();
            before.CopyTo(buffer.Slice(written));
            written += before.Length;
        }

        var branchKey = new string(buffer.Slice(0, written));

        if (rentedBuffer != null)
        {
            ArrayPool<char>.Shared.Return(rentedBuffer);
        }

        return branchKey;
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
