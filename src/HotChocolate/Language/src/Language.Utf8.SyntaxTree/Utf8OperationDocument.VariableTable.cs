using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Language;

public sealed partial class Utf8OperationDocument
{
    /// <summary>
    /// Holds the position-sorted table of variable occurrences recorded while parsing a
    /// <see cref="Utf8OperationDocument"/>. Each occurrence is a site that points at the
    /// name token of a variable, together with a document-scoped ordinal that identifies the
    /// distinct variable name it refers to.
    /// </summary>
    /// <remarks>
    /// A site packs an <c>int</c> name-token position, a <c>ushort</c> ordinal and a
    /// <c>ushort</c> reserved slot. The reserved slot is always zero for now; it is the future
    /// site-kind field for the rename-site generalization, for example fragment names.
    /// </remarks>
    internal struct VariableTable
    {
        // Layout: siteCount site entries of SiteSize bytes, immediately followed by
        // ordinalCount directory entries of DirectoryEntrySize bytes. A site entry is
        // [int position][ushort ordinal][ushort reserved]; a directory entry is
        // [int nameStart][int length].
        private const int SiteSize = 8;
        private const int DirectoryEntrySize = 8;

        private byte[]? _buffer;
        private int _siteCount;
        private int _ordinalCount;
        private readonly bool _pooled;
        private bool _disposed;

        private VariableTable(byte[]? buffer, int siteCount, int ordinalCount, bool pooled)
        {
            _buffer = buffer;
            _siteCount = siteCount;
            _ordinalCount = ordinalCount;
            _pooled = pooled;
        }

        /// <summary>
        /// Gets an empty table that records no variable occurrences.
        /// </summary>
        internal static VariableTable Empty => default;

        /// <summary>
        /// Gets the number of recorded sites.
        /// </summary>
        internal readonly int SiteCount => _siteCount;

        /// <summary>
        /// Gets the number of distinct variable names.
        /// </summary>
        internal readonly int VariableCount => _ordinalCount;

        /// <summary>
        /// Creates a table over <paramref name="buffer"/>. The buffer may be larger than the
        /// logical size; all access is bounded by the counts.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that holds the packed sites and directory.
        /// </param>
        /// <param name="siteCount">
        /// The number of recorded sites.
        /// </param>
        /// <param name="ordinalCount">
        /// The number of distinct variable names.
        /// </param>
        /// <param name="pooled">
        /// When <see langword="true"/>, <paramref name="buffer"/> is returned to
        /// <see cref="ArrayPool{T}.Shared"/> on disposal.
        /// </param>
        internal static VariableTable Create(byte[] buffer, int siteCount, int ordinalCount, bool pooled)
            => new(buffer, siteCount, ordinalCount, pooled);

        /// <summary>
        /// Gets the source offset of the name token of the site at <paramref name="index"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetSitePosition(int index)
        {
            if ((uint)index >= (uint)_siteCount)
            {
                ThrowIndexOutOfRangeOrDisposed(nameof(index));
            }

            return Unsafe.ReadUnaligned<int>(
                ref Unsafe.Add(ref BufferStart(), (nint)(uint)index * SiteSize));
        }

        /// <summary>
        /// Returns the index of the first site whose name-token position is at or after
        /// <paramref name="position"/>, or <see cref="SiteCount"/> when no such site exists.
        /// Sites are position-sorted by construction, so this is a binary search.
        /// </summary>
        internal readonly int FindFirstSiteAtOrAfter(int position)
        {
            var lo = 0;
            var hi = _siteCount;

            while (lo < hi)
            {
                var mid = (int)(((uint)lo + (uint)hi) >> 1);
                if (GetSitePosition(mid) < position)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }

        /// <summary>
        /// Gets the ordinal of the distinct variable name that the site at
        /// <paramref name="index"/> refers to.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetSiteOrdinal(int index)
        {
            if ((uint)index >= (uint)_siteCount)
            {
                ThrowIndexOutOfRangeOrDisposed(nameof(index));
            }

            return Unsafe.ReadUnaligned<ushort>(
                ref Unsafe.Add(ref BufferStart(), ((nint)(uint)index * SiteSize) + 4));
        }

        /// <summary>
        /// Gets the source offset and token length of the name of the first occurrence of the
        /// distinct variable name identified by <paramref name="ordinal"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void GetVariableName(int ordinal, out int nameStart, out int length)
        {
            if ((uint)ordinal >= (uint)_ordinalCount)
            {
                ThrowIndexOutOfRangeOrDisposed(nameof(ordinal));
            }

            ref var entry = ref Unsafe.Add(
                ref BufferStart(),
                ((nint)_siteCount * SiteSize) + ((nint)(uint)ordinal * DirectoryEntrySize));
            nameStart = Unsafe.ReadUnaligned<int>(ref entry);
            length = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref entry, 4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref byte BufferStart()
#if NETSTANDARD2_0
            => ref MemoryMarshal.GetReference(_buffer!.AsSpan());
#else
            => ref MemoryMarshal.GetArrayDataReference(_buffer!);
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly void ThrowIndexOutOfRangeOrDisposed(string paramName)
        {
            if (_buffer is null)
            {
                throw new ObjectDisposedException(nameof(Utf8OperationDocument));
            }

            throw new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// Releases the table buffer, returning it to the pool when it was rented.
        /// </summary>
        internal void Dispose()
        {
            if (!_disposed)
            {
                var buffer = _buffer;
                _buffer = null;
                _siteCount = 0;
                _ordinalCount = 0;

                if (_pooled && buffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                _disposed = true;
            }
        }
    }
}
