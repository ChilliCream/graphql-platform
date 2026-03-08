using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DbRow
    {
        public const int Size = 20;
        public const int UnknownSize = -1;

        // 27 bits for location + 2 bits OpRefType + 3 reserved bits
        private readonly int _locationAndOpRefType;

        // 27 bits for size/length + sign bit for HasComplexChildren + 4 reserved bits
        private readonly int _sizeOrLengthUnion;

        // 27 bits NumberOfRows + 1 reserved bit + 4 bits TokenType
        private readonly int _tokenTypeAndNumberOfRows;

        // 27 bits ParentRow + 5 reserved bits
        private readonly int _parentRow;

        // 15 bits OperationReferenceId + 9 bits Flags + 8 reserved bits
        private readonly int _opRefIdAndFlags;

        public DbRow(
            ElementTokenType tokenType,
            int location,
            int sizeOrLength = 0,
            int parentRow = 0,
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
            Debug.Assert((byte)tokenType < 16);
            Debug.Assert(location is >= 0 and <= 0x07FFFFFF); // 27 bits
            Debug.Assert(sizeOrLength is UnknownSize or >= 0 and <= 0x07FFFFFF); // 27 bits
            Debug.Assert(parentRow is >= 0 and <= 0x07FFFFFF); // 27 bits
            Debug.Assert(operationReferenceId is >= 0 and <= 0x7FFF); // 15 bits
            Debug.Assert(numberOfRows is >= 0 and <= 0x07FFFFFF); // 27 bits
            Debug.Assert((byte)operationReferenceType <= 3); // 2 bits
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            _locationAndOpRefType = location | ((int)operationReferenceType << 27);
            _sizeOrLengthUnion = sizeOrLength;
            _tokenTypeAndNumberOfRows = ((int)tokenType << 28) | (numberOfRows & 0x07FFFFFF);
            _parentRow = parentRow;
            _opRefIdAndFlags = operationReferenceId | ((int)flags << 15);
        }

        /// <summary>
        /// Element token type (includes Reference for composition).
        /// </summary>
        /// <remarks>
        /// 4 bits = possible values
        /// </remarks>
        public ElementTokenType TokenType => (ElementTokenType)(unchecked((uint)_tokenTypeAndNumberOfRows) >> 28);

        /// <summary>
        /// Operation reference type indicating the type of GraphQL operation element.
        /// </summary>
        /// <remarks>
        /// 2 bits = 4 possible values
        /// </remarks>
        public OperationReferenceType OperationReferenceType
            => (OperationReferenceType)((_locationAndOpRefType >> 27) & 0x03);

        /// <summary>
        /// Byte offset in source data OR metaDb row index for references.
        /// </summary>
        /// <remarks>
        /// 27 bits = 134M limit
        /// </remarks>
        public int Location => _locationAndOpRefType & 0x07FFFFFF;

        /// <summary>
        /// Length of data in JSON payload, number of elements if array or number of properties in an object.
        /// </summary>
        /// <remarks>
        /// 27 bits = 134M limit
        /// </remarks>
        public int SizeOrLength => _sizeOrLengthUnion & 0x07FFFFFF;

        /// <summary>
        /// String/PropertyName: Unescaping required.
        /// </summary>
        public bool HasComplexChildren => _sizeOrLengthUnion < 0;

        /// <summary>
        /// Specifies if a size for the item has ben set.
        /// </summary>
        public bool IsUnknownSize => _sizeOrLengthUnion == UnknownSize;

        /// <summary>
        /// Number of metadb rows this element spans.
        /// </summary>
        /// <remarks>
        /// 27 bits = 134M rows
        /// </remarks>
        public int NumberOfRows => _tokenTypeAndNumberOfRows & 0x07FFFFFF;

        /// <summary>
        /// Index of parent element in metadb for navigation and null propagation.
        /// </summary>
        /// <remarks>
        /// 27 bits = 134M rows
        /// </remarks>
        public int ParentRow => _parentRow & 0x07FFFFFF;

        /// <summary>
        /// Reference to GraphQL selection set or selection metadata.
        /// 15 bits = 32K selections
        /// </summary>
        /// <remarks>
        /// 15 bits = 32K selections
        /// </remarks>
        public int OperationReferenceId => _opRefIdAndFlags & 0x7FFF;

        /// <summary>
        /// Element metadata flags.
        /// </summary>
        /// <remarks>
        /// 9 bits = 512 combinations
        /// </remarks>
        public ElementFlags Flags => (ElementFlags)((_opRefIdAndFlags >> 15) & 0x1FF);

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
    internal enum ElementFlags : short
    {
        None = 0,
        IsRoot = 1,
        IsObject = 2,
        IsList = 4,
        IsInternal = 8,
        IsExcluded = 16,
        IsNullable = 32,
        IsInvalidated = 64,
        IsEncoded = 128,
        IsDeferred = 256
    }
}
