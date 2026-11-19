using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DbRow
    {
        internal const int Size = 12;

        // Sign bit is currently unassigned
        private readonly int _location;

        // Sign bit is used for "HasComplexChildren" (StartArray)
        private readonly int _sizeOrLengthUnion;

        // Top nybble is JsonTokenType
        // remaining nybbles hold the JSON number format for numbers, or the row count for other tokens
        // This isn't limiting on the number of rows, since Span.MaxLength / sizeof(DbRow) can't
        // exceed that range.
        private readonly int _numberOfRowsAndTypeUnion;

        internal DbRow(
            JsonTokenType jsonTokenType,
            int location,
            int sizeOrLength,
            int rowsOrNumberFormat,
            bool hasComplexChildren)
        {
            Debug.Assert(jsonTokenType is > JsonTokenType.None and <= JsonTokenType.Null);
            Debug.Assert((byte)jsonTokenType < 1 << 4);
            Debug.Assert(location >= 0);
            Debug.Assert(sizeOrLength >= UnknownSize);
            Debug.Assert(rowsOrNumberFormat is >= 1 and <= 0x0FFFFFFF);
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            _location = location;
            _sizeOrLengthUnion = hasComplexChildren
                // Set sign bit
                ? sizeOrLength | int.MinValue
                // Clear sign bit
                : sizeOrLength & int.MaxValue;
            _numberOfRowsAndTypeUnion = ((int)jsonTokenType << 28) | rowsOrNumberFormat;
        }

        /// <summary>
        /// Index into the payload
        /// </summary>
        internal int Location => _location;

        /// <summary>
        /// length of text in JSON payload (or number of elements if it's a JSON array)
        /// </summary>
        internal int SizeOrLength => _sizeOrLengthUnion & int.MaxValue;

        internal bool IsUnknownSize => _sizeOrLengthUnion == UnknownSize;

        /// <summary>
        /// String/PropertyName: Unescaping is required.
        /// Array: At least one element is an object/array.
        /// Otherwise; false
        /// </summary>
        internal bool HasComplexChildren => _sizeOrLengthUnion < 0;

        internal int NumberOfRows => TokenType is JsonTokenType.Number
            ? 1
            : _numberOfRowsAndTypeUnion & 0x0FFFFFFF;

        internal NumberFormat NumberFormat
        {
            get
            {
                Debug.Assert(TokenType is JsonTokenType.Number);
                return (NumberFormat)(_numberOfRowsAndTypeUnion & 0x0FFFFFFF);
            }
        }

        internal JsonTokenType TokenType => (JsonTokenType)(unchecked((uint)_numberOfRowsAndTypeUnion) >> 28);

        internal const int UnknownSize = -1;

        internal const int NoLocation = -1;

        internal bool IsSimpleValue => TokenType >= JsonTokenType.PropertyName;
    }

    internal enum NumberFormat
    {
        Int32 = 1,
        Decimal,
        PotentialDoubleOverflow
    }
}
