using System.Collections.Immutable;
using System.Globalization;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Builds the cache key that identifies an alias batched operation by the inbound requests it
/// was merged from. The key combines each request's operation hash with its row count so that
/// two batches sharing the same operations and cardinalities reuse the same rewritten document.
/// </summary>
/// <remarks>
/// The key has the shape <c>hash_0:hash_1:...:hash_{M-1}|n_0,n_1,...,n_{M-1}</c>, where each
/// hash is the request's operation hash formatted as 16 hexadecimal characters and each
/// <c>n_i</c> is the request's row count. Callers stack allocate a destination of
/// <see cref="GetMaxKeyLength"/> characters for the batch and slice it to the returned length.
/// A <c>stackalloc char[192]</c> covers any batch of up to eight operations.
/// </remarks>
internal static class AliasBatchCacheKey
{
    /// <summary>
    /// Writes the cache key for the given requests into <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">
    /// The buffer that receives the key. It must be at least <see cref="GetMaxKeyLength"/>
    /// characters long for the given <paramref name="requests"/>.
    /// </param>
    /// <param name="requests">The inbound requests the key identifies.</param>
    /// <returns>The number of characters written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="requests"/> is empty or when <paramref name="destination"/>
    /// is too small to hold the key.
    /// </exception>
    public static int Build(Span<char> destination, ImmutableArray<SourceSchemaClientRequest> requests)
    {
        if (requests.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "Alias batching requires at least one request.",
                nameof(requests));
        }

        var position = 0;

        for (var i = 0; i < requests.Length; i++)
        {
            if (i > 0)
            {
                Write(destination, ref position, ':');
            }

            if (!requests[i].OperationHash.TryFormat(
                destination[position..],
                out var hashWritten,
                "x16",
                CultureInfo.InvariantCulture))
            {
                throw new ArgumentException(
                    "The destination buffer is too small to hold the cache key.",
                    nameof(destination));
            }

            position += hashWritten;
        }

        Write(destination, ref position, '|');

        for (var i = 0; i < requests.Length; i++)
        {
            if (i > 0)
            {
                Write(destination, ref position, ',');
            }

            // A request with an empty variable set still counts as one row.
            var rowCount = Math.Max(1, requests[i].Variables.Length);

            if (!rowCount.TryFormat(
                destination[position..],
                out var rowsWritten,
                provider: CultureInfo.InvariantCulture))
            {
                throw new ArgumentException(
                    "The destination buffer is too small to hold the cache key.",
                    nameof(destination));
            }

            position += rowsWritten;
        }

        return position;
    }

    /// <summary>
    /// Gets the maximum number of characters the key for the given requests can occupy.
    /// </summary>
    /// <param name="requests">The inbound requests the key identifies.</param>
    /// <returns>The upper bound on the key length in characters.</returns>
    public static int GetMaxKeyLength(ImmutableArray<SourceSchemaClientRequest> requests)
    {
        if (requests.IsDefaultOrEmpty)
        {
            return 0;
        }

        var count = requests.Length;

        // hashes (16 chars each) + colon separators between them
        // + one pipe
        // + row counts (up to 10 digits each for int.MaxValue) + comma separators between them
        return (count * 16) + (count - 1)
            + 1
            + (count * 10) + (count - 1);
    }

    private static void Write(Span<char> destination, ref int position, char value)
    {
        if (position >= destination.Length)
        {
            throw new ArgumentException(
                "The destination buffer is too small to hold the cache key.",
                nameof(destination));
        }

        destination[position++] = value;
    }
}
