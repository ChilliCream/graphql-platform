using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Text.Json;

public sealed partial class CompositeResultDocument
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DbRow
    {
        public const int Size = 20;
        public const int UnknownSize = -1;

        // 28 bits for location/reference + 4 reserved bits
        private readonly int _locationAndReserved;

        // Sign bit for HasComplexChildren + 31 bits for size/length
        private readonly int _sizeOrLengthUnion;

        // 4 bits TokenType + 25 bits NumberOfRows + 3 reserved bits
        private readonly int _numberOfRowsTypeAndReserved;

        // 16 bits SourceDocumentId + 16 bits (high 16 bits of ParentRow)
        private readonly int _sourceAndParentHigh;

        // 15 bits SelectionSetId + 4 bits Flags + 12 bits (low bits of ParentRow) + 1 reserved bit
        private readonly int _selectionSetFlagsAndParentLow;

        public DbRow(
            ElementTokenType tokenType,
            int location,
            int sizeOrLength = 0,
            int sourceDocumentId = 0,
            int parentRow = 0,
            int selectionSetId = 0,
            ElementFlags flags = ElementFlags.None)
        {
            Debug.Assert(tokenType is > ElementTokenType.None and <= ElementTokenType.Reference);
            Debug.Assert((byte)tokenType < 16);
            Debug.Assert(location is >= 0 and <= 0x0FFFFFFF); // 28 bits
            Debug.Assert(sizeOrLength >= UnknownSize);
            Debug.Assert(sourceDocumentId is >= 0 and <= 0xFFFF); // 16 bits
            Debug.Assert(parentRow is >= 0 and <= 0x0FFFFFFF); // 28 bits
            Debug.Assert(selectionSetId is >= 0 and <= 0x7FFF); // 15 bits
            Debug.Assert((byte)flags <= 15); // 4 bits
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            _locationAndReserved = location; // Uses bottom 28 bits
            _sizeOrLengthUnion = sizeOrLength;
            _numberOfRowsTypeAndReserved = (int)tokenType << 28;
            _sourceAndParentHigh = sourceDocumentId | ((parentRow >> 12) << 16);
            _selectionSetFlagsAndParentLow = selectionSetId | ((int)flags << 15) | ((parentRow & 0xFFF) << 19);
        }

        /// <summary>
        /// Byte offset in source data OR metadb row index for references.
        /// 28 bits = 268M limit
        /// </summary>
        public int Location => _locationAndReserved & 0x0FFFFFFF;

        /// <summary>
        /// Length of data in JSON payload or number of elements if array.
        /// 31 bits = 2GB limit
        /// </summary>
        public int SizeOrLength => _sizeOrLengthUnion & int.MaxValue;

        /// <summary>
        /// String/PropertyName: Unescaping required.
        /// Array: Contains complex children.
        /// </summary>
        public bool HasComplexChildren => _sizeOrLengthUnion < 0;

        public bool IsUnknownSize => _sizeOrLengthUnion == UnknownSize;

        /// <summary>
        /// Element token type (includes Reference for composition).
        /// 4 bits = 16 types
        /// </summary>
        public ElementTokenType TokenType => (ElementTokenType)(unchecked((uint)_numberOfRowsTypeAndReserved) >> 28);

        /// <summary>
        /// Number of metadb rows this element spans.
        /// 25 bits = 33M rows
        /// </summary>
        public int NumberOfRows => _numberOfRowsTypeAndReserved & 0x01FFFFFF;

        /// <summary>
        /// Which source JSON document contains the data.
        /// 16 bits = 65K documents
        /// </summary>
        public int SourceDocumentId => _sourceAndParentHigh & 0xFFFF;

        /// <summary>
        /// Index of parent element in metadb for navigation and null propagation.
        /// 28 bits = 268M rows (reconstructed from high+low bits)
        /// </summary>
        public int ParentRow
            => ((_sourceAndParentHigh >> 16) << 12) | ((_selectionSetFlagsAndParentLow >> 19) & 0xFFF);

        /// <summary>
        /// Reference to GraphQL selection set metadata.
        /// 15 bits = 32K selections
        /// </summary>
        public int SelectionSetId => _selectionSetFlagsAndParentLow & 0x7FFF;

        /// <summary>
        /// Element metadata flags.
        /// 4 bits = 16 combinations
        /// </summary>
        public ElementFlags Flags => (ElementFlags)((_selectionSetFlagsAndParentLow >> 15) & 0xF);

        /// <summary>
        /// True for primitive JSON values (strings, numbers, booleans, null).
        /// </summary>
        public bool IsSimpleValue => TokenType >= ElementTokenType.PropertyName;
    }

    [Flags]
    internal enum ElementFlags : byte
    {
        None = 0,

        // 0x01 - For error propagation
        Invalidated = 1,

        // 0x02 - Data stored in composite (not source document)
        Local = 2,

        // 0x04 - Field can be null (schema info)
        IsNullable = 4,

        // 0x08 - Element has no parent (ignore ParentRow value)
        IsRoot = 8,

        // 0x16 - Element is internal and mustnt be written to the output stream.
        IsInternal = 16,

        IsLeaf = 32
    }

    internal enum ElementTokenType : byte
    {
        None = 0,
        StartObject = 1,
        EndObject = 2,
        StartArray = 3,
        EndArray = 4,
        PropertyName = 5,
        // Retained for compatibility, we do not actually need this.
        Comment = 6,
        String = 7,
        Number = 8,
        True = 9,
        False = 10,
        Null = 11,
        // A reference in case a property or array element point
        // to an array or an object
        Reference = 12
    }
}
