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
        internal const int TypeAndParentOffset = 0;
        internal const int SelectionAndFlagsOffset = 4;
        internal const int SizeOffset = 8;
        internal const int LocationOrRowsOffset = 12;
        internal const int SourceOffset = 16;

        // 4 bits TokenType + 28 bits ParentRow
        private readonly int _typeAndParent;

        // 15 bits OperationReferenceId + 2 bits OperationReferenceType + 6 bits Flags + 9 reserved
        private readonly int _selectionAndFlags;

        // 1 bit HasComplexChildren (sign) + 31 bits SizeOrLength
        private readonly int _sizeOrLengthUnion;

        // 27 bits — either Location or NumberOfRows, depending on TokenType/Flags
        private readonly int _locationOrRows;

        // 15 bits SourceDocumentId + 17 reserved
        private readonly int _source;

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
            Debug.Assert(location is >= 0 and <= 0x07FFFFFF); // 27 bits
            Debug.Assert(sizeOrLength >= UnknownSize);
            Debug.Assert(sourceDocumentId is >= 0 and <= 0x7FFF); // 15 bits
            Debug.Assert(parentRow is >= 0 and <= 0x0FFFFFFF); // 28 bits
            Debug.Assert(operationReferenceId is >= 0 and <= 0x7FFF); // 15 bits
            Debug.Assert(numberOfRows is >= 0 and <= 0x07FFFFFF); // 27 bits
            Debug.Assert((byte)flags <= 63); // 6 bits (0x3F)
            Debug.Assert((byte)operationReferenceType <= 3); // 2 bits
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            var locationOrRows = location != 0 ? location : numberOfRows;

            _typeAndParent = ((int)tokenType & 0x0F) | (parentRow << 4);
            _selectionAndFlags = operationReferenceId
                | ((int)operationReferenceType << 15)
                | ((int)flags << 17);
            _sizeOrLengthUnion = sizeOrLength;
            _locationOrRows = locationOrRows & 0x07FFFFFF;
            _source = sourceDocumentId & 0x7FFF;
        }

        /// <summary>
        /// Element token type (includes Reference for composition).
        /// </summary>
        /// <remarks>
        /// 4 bits = 16 possible values
        /// </remarks>
        public ElementTokenType TokenType => (ElementTokenType)(_typeAndParent & 0x0F);

        /// <summary>
        /// Operation reference type indicating the type of GraphQL operation element.
        /// </summary>
        /// <remarks>
        /// 2 bits = 4 possible values
        /// </remarks>
        public OperationReferenceType OperationReferenceType
            => (OperationReferenceType)((_selectionAndFlags >> 15) & 0x03);

        /// <summary>
        /// Byte offset in source data or metaDb row index for references.
        /// </summary>
        /// <remarks>
        /// 27 bits = 134M limit
        /// </remarks>
        public int Location => _locationOrRows & 0x07FFFFFF;

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
        /// 27 bits = 134M rows
        /// </remarks>
        public int NumberOfRows => _locationOrRows & 0x07FFFFFF;

        /// <summary>
        /// Which source JSON document contains the data.
        /// </summary>
        /// <remarks>
        /// 15 bits = 32K documents
        /// </remarks>
        public int SourceDocumentId => _source & 0x7FFF;

        /// <summary>
        /// Index of parent element in metadb for navigation and null propagation.
        /// </summary>
        /// <remarks>
        /// 28 bits = 268M rows
        /// </remarks>
        public int ParentRow => (int)((uint)_typeAndParent >> 4);

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
        /// 6 bits = 64 combinations
        /// </remarks>
        public ElementFlags Flags => (ElementFlags)((_selectionAndFlags >> 17) & 0x3F);

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
        IsExcluded = 32
    }
}
