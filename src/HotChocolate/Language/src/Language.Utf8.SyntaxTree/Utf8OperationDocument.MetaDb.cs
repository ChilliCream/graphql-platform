using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Language;

public sealed partial class Utf8OperationDocument
{
    /// <summary>
    /// Holds the packed metadata rows that describe the structure of a
    /// <see cref="Utf8OperationDocument"/>.
    /// </summary>
    internal struct MetaDb
    {
        private byte[]? _buffer;
        private readonly int _length;
        private readonly bool _pooled;
        private int _rowCount;

        private MetaDb(byte[] buffer, int length, bool pooled)
        {
            _buffer = buffer;
            _length = length;
            _pooled = pooled;
            _rowCount = length / DbRow.Size;
        }

        /// <summary>
        /// Gets the logical byte length of the metadata.
        /// </summary>
        internal readonly int Length => _length;

        /// <summary>
        /// Gets the number of rows stored in the metadata.
        /// </summary>
        internal readonly int RowCount => _rowCount;

        /// <summary>
        /// Creates a metadata database over <paramref name="buffer"/>. The buffer may be larger
        /// than <paramref name="length"/>; all access is bounded by <paramref name="length"/>.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that holds the packed rows.
        /// </param>
        /// <param name="length">
        /// The logical byte length, which must be a whole number of rows.
        /// </param>
        /// <param name="pooled">
        /// When <see langword="true"/>, <paramref name="buffer"/> is returned to
        /// <see cref="ArrayPool{T}.Shared"/> on disposal.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="length"/> is not a whole number of rows.
        /// </exception>
        internal static MetaDb Create(byte[] buffer, int length, bool pooled)
        {
            if (length % DbRow.Size != 0)
            {
                throw new ArgumentException("The metadata is not row aligned.", nameof(length));
            }

            return new MetaDb(buffer, length, pooled);
        }

        /// <summary>
        /// Reads the row at <paramref name="index"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly DbRow GetRow(int index)
        {
            if ((uint)index >= (uint)_rowCount)
            {
                ThrowIndexOutOfRangeOrDisposed();
            }

#if NETSTANDARD2_0
            ref var start = ref MemoryMarshal.GetReference(_buffer!.AsSpan());
#else
            ref var start = ref MemoryMarshal.GetArrayDataReference(_buffer!);
#endif
            return Unsafe.ReadUnaligned<DbRow>(
                ref Unsafe.Add(ref start, (nint)(uint)index * DbRow.Size));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly void ThrowIndexOutOfRangeOrDisposed()
        {
            if (_buffer is null)
            {
                throw new ObjectDisposedException(nameof(Utf8OperationDocument));
            }

            throw new ArgumentOutOfRangeException("index");
        }

        /// <summary>
        /// Releases the metadata buffer, returning it to the pool when it was rented.
        /// </summary>
        internal void Dispose()
        {
            var buffer = _buffer;
            _buffer = null;
            _rowCount = 0;

            if (_pooled && buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
