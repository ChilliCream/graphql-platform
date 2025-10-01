using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DbRow
    {
        public const int Size = 20;
        public const int UnknownSize = -1;

        // 27 bits for location + 2 bits OpRefType + 3 reserved bits
        private readonly int _locationAndOpRefType;

        // Sign bit for HasComplexChildren + 31 bits for size/length
        private readonly int _sizeOrLengthUnion;

        // 4 bits TokenType + 27 bits NumberOfRows + 1 reserved bit
        private readonly int _numberOfRowsTypeAndReserved;

        // 15 bits SourceDocumentId + 17 bits (high 17 bits of ParentRow)
        private readonly int _sourceAndParentHigh;

        // 15 bits OperationReferenceId + 6 bits Flags + 11 bits (low bits of ParentRow)
        private readonly int _selectionSetFlagsAndParentLow;

        public DbRow(
            ElementTokenType tokenType,
            int location,
            int sizeOrLength = 0,
            int sourceDocumentId = 0,
            int parentRow = 0,
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
            Debug.Assert((byte)tokenType < 16);
            Debug.Assert(location is >= 0 and <= 0x07FFFFFF); // 27 bits
            Debug.Assert(sizeOrLength >= UnknownSize);
            Debug.Assert(sourceDocumentId is >= 0 and <= 0x7FFF); // 15 bits
            Debug.Assert(parentRow is >= 0 and <= 0x0FFFFFFF); // 28 bits
            Debug.Assert(operationReferenceId is >= 0 and <= 0x7FFF); // 15 bits
            Debug.Assert(numberOfRows is >= 0 and <= 0x07FFFFFF); // 27 bits
            Debug.Assert((byte)flags <= 63); // 6 bits (0x3F)
            Debug.Assert((byte)operationReferenceType <= 3); // 2 bits
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            _locationAndOpRefType = location | ((int)operationReferenceType << 27);
            _sizeOrLengthUnion = sizeOrLength;
            _numberOfRowsTypeAndReserved = ((int)tokenType << 28) | (numberOfRows & 0x07FFFFFF);
            _sourceAndParentHigh = sourceDocumentId | ((parentRow >> 11) << 15);
            _selectionSetFlagsAndParentLow = operationReferenceId | ((int)flags << 15) | ((parentRow & 0x7FF) << 21);
        }

        /// <summary>
        /// Byte offset in source data OR metadb row index for references.
        /// 27 bits = 134M limit (increased from 26 bits / 67M limit)
        /// </summary>
        public int Location => _locationAndOpRefType & 0x07FFFFFF;

        /// <summary>
        /// Operation reference type indicating the type of GraphQL operation element.
        /// 2 bits = 4 possible values
        /// </summary>
        public OperationReferenceType OperationReferenceType
            => (OperationReferenceType)((_locationAndOpRefType >> 27) & 0x03);

        /// <summary>
        /// Length of data in JSON payload, number of elements if array or number of properties in an object.
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
        /// 27 bits = 134M rows
        /// </summary>
        public int NumberOfRows => _numberOfRowsTypeAndReserved & 0x07FFFFFF;

        /// <summary>
        /// Which source JSON document contains the data.
        /// 15 bits = 32K documents
        /// </summary>
        public int SourceDocumentId => _sourceAndParentHigh & 0x7FFF;

        /// <summary>
        /// Index of parent element in metadb for navigation and null propagation.
        /// 28 bits = 268M rows (reconstructed from high+low bits)
        /// </summary>
        public int ParentRow
            => ((int)((uint)_sourceAndParentHigh >> 15) << 11) | ((_selectionSetFlagsAndParentLow >> 21) & 0x7FF);

        /// <summary>
        /// Reference to GraphQL selection set or selection metadata.
        /// 15 bits = 32K selections
        /// </summary>
        public int OperationReferenceId => _selectionSetFlagsAndParentLow & 0x7FFF;

        /// <summary>
        /// Element metadata flags.
        /// 6 bits = 64 combinations
        /// </summary>
        public ElementFlags Flags => (ElementFlags)((_selectionSetFlagsAndParentLow >> 15) & 0x3F);

        /// <summary>
        /// True for primitive JSON values (strings, numbers, booleans, null).
        /// </summary>
        public bool IsSimpleValue => TokenType is >= ElementTokenType.PropertyName and <= ElementTokenType.Null;
    }

    internal enum OperationReferenceType : byte
    {
        None = 0,
        SelectionSet = 1,
        Selection = 2
    }

    [Flags]
    internal enum ElementFlags : byte
    {
        None = 0,
        Invalidated = 1,
        SourceResult = 2,
        IsNullable = 4,
        IsRoot = 8,
        IsInternal = 16,
        IsLeaf = 32
    }
}
