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

        // Sign bit is currently unassigned
        private readonly int _location;

        // Sign bit is currently unassigned
        private readonly int _sizeOrLength;

        // Top nybble is Utf8SyntaxKind
        // remaining nybbles are the number of rows this node spans (including its own)
        private readonly int _kindAndNumberOfRows;

        internal DbRow(
            Utf8SyntaxKind kind,
            int location,
            int sizeOrLength,
            int numberOfRows)
        {
            Debug.Assert(kind is > Utf8SyntaxKind.None and <= Utf8SyntaxKind.Alias);
            Debug.Assert((byte)kind < 1 << 4);
            Debug.Assert(location >= 0);
            Debug.Assert(sizeOrLength >= 0);
            Debug.Assert(numberOfRows is >= 1 and <= 0x0FFFFFFF);
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            _location = location;
            _sizeOrLength = sizeOrLength;
            _kindAndNumberOfRows = ((int)kind << 28) | numberOfRows;
        }

        /// <summary>
        /// Byte offset of the first byte of this node's source text.
        /// </summary>
        internal int Location => _location;

        /// <summary>
        /// Byte length of this node's source text. For <see cref="Utf8SyntaxKind.Name"/>,
        /// <see cref="Utf8SyntaxKind.Alias"/> and <see cref="Utf8SyntaxKind.TypeCondition"/>
        /// rows this is the token length; for every other kind it is the length of the node's
        /// source range.
        /// </summary>
        internal int SizeOrLength => _sizeOrLength;

        /// <summary>
        /// Exclusive byte offset one past the last byte of this node's source text.
        /// </summary>
        internal int SourceEnd => _location + _sizeOrLength;

        /// <summary>
        /// Number of metadata rows this node spans, including the node itself.
        /// </summary>
        internal int NumberOfRows => _kindAndNumberOfRows & 0x0FFFFFFF;

        /// <summary>
        /// The kind of syntax node represented by this row.
        /// </summary>
        internal Utf8SyntaxKind Kind
            => (Utf8SyntaxKind)(unchecked((uint)_kindAndNumberOfRows) >> 28);
    }
}
