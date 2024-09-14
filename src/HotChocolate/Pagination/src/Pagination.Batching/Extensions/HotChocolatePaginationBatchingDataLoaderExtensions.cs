using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using GreenDonut.Projections;
using HotChocolate.Pagination;

namespace GreenDonut;

/// <summary>
/// Provides extension methods to pass a pagination context to a DataLoader.
/// </summary>
public static class HotChocolatePaginationBatchingDataLoaderExtensions
{
    /// <summary>
    /// Branches a DataLoader with the provided <see cref="PagingArguments"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader that shall be branched.
    /// </param>
    /// <param name="pagingArguments">
    /// The paging arguments that shall be exist as state in the branched DataLoader.
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
            var branch = new PagingDataLoader<TKey, Page<TValue>>(
                (DataLoaderBase<TKey, Page<TValue>>)root,
                branchKey);
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
    /// <returns>
    /// Returns the DataLoader with the added projection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
#if NET8_0_OR_GREATER
    [Experimental(Experiments.Projections)]
#endif
    public static IPagingDataLoader<TKey, Page<TValue>> Select<TKey, TValue>(
        this IPagingDataLoader<TKey, Page<TValue>> dataLoader,
        Expression<Func<TValue, TValue>> selector)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var builder = dataLoader.GetOrSetState(_ => new DefaultSelectorBuilder<TValue>());
        builder.Add(selector);
        return dataLoader;
    }

    private static string CreateBranchKey(
        PagingArguments pagingArguments)
    {
        var requiredBufferSize = 1;

        requiredBufferSize += EstimateIntLength(pagingArguments.First);
        requiredBufferSize += pagingArguments.After?.Length ?? 0;
        requiredBufferSize += EstimateIntLength(pagingArguments.Last);
        requiredBufferSize += pagingArguments.Before?.Length ?? 0;

        if (requiredBufferSize == 1)
        {
            return "-";
        }

        char[]? rentedBuffer = null;
        Span<char> buffer = requiredBufferSize <= 128
            ? stackalloc char[requiredBufferSize]
            : (rentedBuffer = ArrayPool<char>.Shared.Rent(requiredBufferSize));

        var written = 1;
        buffer[0] = '-';

        if (pagingArguments.First.HasValue)
        {
            if (!pagingArguments.First.Value.TryFormat(buffer.Slice(written), out var charsWritten))
            {
                throw new InvalidOperationException("Buffer is too small.");
            }
            written += charsWritten;
        }

        if (pagingArguments.After != null)
        {
            var after = pagingArguments.After.AsSpan();
            after.CopyTo(buffer.Slice(written));
            written += after.Length;
        }

        if (pagingArguments.Last.HasValue)
        {
            if (!pagingArguments.Last.Value.TryFormat(buffer.Slice(written), out var charsWritten))
            {
                throw new InvalidOperationException("Buffer is too small.");
            }
            written += charsWritten;
        }

        if (pagingArguments.Before != null)
        {
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
        if(value is null)
        {
            return 0;
        }

        if (value == 0)
        {
            // to print 0 we need still 1 digit
            return 1;
        }

        // if the number is negative we need one more digit for the sign
        var length = (value < 0) ? 1 : 0;

        // we add the number of digits the number has to the length of the number.
        length += (int)Math.Floor(Math.Log10(Math.Abs(value.Value)) + 1);

        return length;
    }
}
