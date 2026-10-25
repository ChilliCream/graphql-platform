using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Language;

public sealed partial class Utf8OperationDocument
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DbRow
    {
        internal const int Size = 12;

        /// <summary>
        /// The largest number of rows a single node can span, including its own.
        /// </summary>
        internal const int MaxNumberOfRows = 0x07FFFFFF;

        /// <summary>
        /// Selects the row count from <see cref="_kindAndNumberOfRows"/>. The row count occupies
        /// the low 27 bits, so the mask and the largest representable row count are the same value.
        /// </summary>
        private const int NumberOfRowsMask = MaxNumberOfRows;

        // Sign bit is currently unassigned
        private readonly int _location;

        // Sign bit is currently unassigned
        private readonly int _sizeOrLength;

        // Top five bits are the Utf8SyntaxKind
        // the remaining 27 bits are the number of rows this node spans (including its own)
        private readonly int _kindAndNumberOfRows;

        internal DbRow(
            Utf8SyntaxKind kind,
            int location,
            int sizeOrLength,
            int numberOfRows)
        {
            Debug.Assert(kind is > Utf8SyntaxKind.None and <= Utf8SyntaxKind.NonNullType);
            Debug.Assert((byte)kind < 1 << 5);
            Debug.Assert(location >= 0);
            Debug.Assert(sizeOrLength >= 0);
            Debug.Assert(numberOfRows is >= 1 and <= MaxNumberOfRows);
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            _location = location;
            _sizeOrLength = sizeOrLength;
            _kindAndNumberOfRows = ((int)kind << 27) | numberOfRows;
        }

        /// <summary>
        /// Byte offset of the first byte of this node's source text.
        /// </summary>
        internal int Location => _location;

        /// <summary>
        /// Byte length of this node's source text. For a row that represents a single token this
        /// is the token length; for every other kind it is the length of the node's source range.
        /// </summary>
        internal int SizeOrLength => _sizeOrLength;

        /// <summary>
        /// Exclusive byte offset one past the last byte of this node's source text.
        /// </summary>
        internal int SourceEnd => _location + _sizeOrLength;

        /// <summary>
        /// Number of metadata rows this node spans, including the node itself.
        /// </summary>
        internal int NumberOfRows => _kindAndNumberOfRows & NumberOfRowsMask;

        /// <summary>
        /// The kind of syntax node represented by this row.
        /// </summary>
        internal Utf8SyntaxKind Kind
            => (Utf8SyntaxKind)(unchecked((uint)_kindAndNumberOfRows) >> 27);
    }
}
