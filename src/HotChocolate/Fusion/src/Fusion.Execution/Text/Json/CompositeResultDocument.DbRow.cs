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

        // Byte offsets used by MetaDb's direct-read fast paths.
        internal const int ParentOffset = 0;
        internal const int SelectionAndFlagsOffset = 4;
        internal const int SizeOffset = 8;
        internal const int LocationOrRowsOffset = 12;
        internal const int SourceAndTypeOffset = 16;

        // 29 bits parent cursor value + 3 reserved
        private readonly int _parent;

        // 15 bits OperationReferenceId + 2 bits OperationReferenceType + 7 bits Flags + 8 reserved
        private readonly int _selectionAndFlags;

        // 1 bit HasComplexChildren (sign) + 31 bits SizeOrLength
        private readonly int _sizeOrLengthUnion;

        // 29 bits, either Location or NumberOfRows, depending on TokenType/Flags
        private readonly int _locationOrRows;

        // 15 bits SourceDocumentId + 4 bits TokenType + 13 reserved
        private readonly int _sourceAndType;

        public DbRow(
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int sourceDocumentId = 0,
            int parentRow = 0,
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
            Debug.Assert((byte)tokenType < 16);
            Debug.Assert(location is >= 0 and <= 0x1FFFFFFF); // 29 bits
            Debug.Assert(sizeOrLength >= UnknownSize);
            Debug.Assert(sourceDocumentId is >= 0 and <= 0x7FFF); // 15 bits
            Debug.Assert(parentRow is >= 0 and <= 0x1FFFFFFF); // 29 bits (cursor value)
            Debug.Assert(operationReferenceId is >= 0 and <= 0x7FFF); // 15 bits
            Debug.Assert(numberOfRows is >= 0 and <= 0x1FFFFFFF); // 29 bits
            Debug.Assert((byte)flags <= 127); // 7 bits (0x7F)
            Debug.Assert((byte)operationReferenceType <= 3); // 2 bits
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            var locationOrRows = location != 0 ? location : numberOfRows;

            _parent = parentRow & 0x1FFFFFFF;
            _selectionAndFlags = operationReferenceId
                | ((int)operationReferenceType << 15)
                | ((int)flags << 17);
            _sizeOrLengthUnion = sizeOrLength;
            _locationOrRows = locationOrRows & 0x1FFFFFFF;
            _sourceAndType = (sourceDocumentId & 0x7FFF) | (((int)tokenType & 0x0F) << 15);
        }

        /// <summary>
        /// Element token type (includes Reference for composition).
        /// </summary>
        /// <remarks>
        /// 4 bits = 16 possible values
        /// </remarks>
        public ElementTokenType TokenType => (ElementTokenType)((_sourceAndType >>> 15) & 0x0F);

        /// <summary>
        /// Operation reference type indicating the type of GraphQL operation element.
        /// </summary>
        /// <remarks>
        /// 2 bits = 4 possible values
        /// </remarks>
        public OperationReferenceType OperationReferenceType
            => (OperationReferenceType)((_selectionAndFlags >> 15) & 0x03);

        /// <summary>
        /// Byte offset in source data, or the packed cursor value of the target row for references.
        /// </summary>
        /// <remarks>
        /// 29 bits
        /// </remarks>
        public int Location => _locationOrRows & 0x1FFFFFFF;

        /// <summary>
        /// Length of data in JSON payload, number of elements if array or number of properties in an object.
        /// </summary>
        /// <remarks>
        /// 31 bits = 2GB limit
        /// </remarks>
        public int SizeOrLength => _sizeOrLengthUnion & int.MaxValue;

        /// <summary>
        /// String/PropertyName: Unescaping required.
        /// </summary>
        public bool HasComplexChildren => _sizeOrLengthUnion < 0;

        /// <summary>
        /// Specifies if a size for the item has not been set.
        /// </summary>
        public bool IsUnknownSize => _sizeOrLengthUnion == UnknownSize;

        /// <summary>
        /// Number of metadb rows this element spans.
        /// </summary>
        /// <remarks>
        /// 29 bits
        /// </remarks>
        public int NumberOfRows => _locationOrRows & 0x1FFFFFFF;

        /// <summary>
        /// Which source JSON document contains the data.
        /// </summary>
        /// <remarks>
        /// 15 bits = 32K documents
        /// </remarks>
        public int SourceDocumentId => _sourceAndType & 0x7FFF;

        /// <summary>
        /// Packed cursor value of the parent element, used for navigation and null propagation.
        /// </summary>
        /// <remarks>
        /// 29 bits
        /// </remarks>
        public int Parent => _parent & 0x1FFFFFFF;

        /// <summary>
        /// Reference to GraphQL selection set or selection metadata.
        /// </summary>
        /// <remarks>
        /// 15 bits = 32K selections
        /// </remarks>
        public int OperationReferenceId => _selectionAndFlags & 0x7FFF;

        /// <summary>
        /// Element metadata flags.
        /// </summary>
        /// <remarks>
        /// 7 bits = 128 combinations
        /// </remarks>
        public ElementFlags Flags => (ElementFlags)((_selectionAndFlags >> 17) & 0x7F);

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
        IsExcluded = 32,
        IsEnumValue = 64
    }
}
