using static HotChocolate.Fusion.Text.Json.CompositeResultDocument;

namespace HotChocolate.Fusion.Text.Json;

public class CompositeResultDocumentMetaDbTests : IDisposable
{
    private MetaDb _metaDb = MetaDb.CreateForEstimatedRows(100);

    [Fact]
    public void CreateForEstimatedRows_WithSmallEstimate_CreatesValidMetaDb()
    {
        // Arrange & Act
        using var metaDb = MetaDb.CreateForEstimatedRows(10);

        // Assert
        Assert.Equal(0, metaDb.NextCursor.Index);
    }

    [Fact]
    public void Append_SingleRow_ReturnsCorrectIndex()
    {
        // Arrange & Act
        var cursor = _metaDb.Append(
            ElementTokenType.String,
            location: 42,
            sizeOrLength: 10,
            sourceDocumentId: 1,
            parentRow: 0,
            operationReferenceId: 5,
            flags: ElementFlags.None);

        // Assert
        Assert.Equal(0, cursor.Index);
        Assert.Equal(20, _metaDb.NextCursor.ToTotalBytes());
    }

    [Fact]
    public void Append_MultipleRows_ReturnsSequentialIndices()
    {
        // Arrange & Act
        var index1 = _metaDb.Append(ElementTokenType.StartObject);
        var index2 = _metaDb.Append(ElementTokenType.String, location: 10);
        var index3 = _metaDb.Append(ElementTokenType.EndObject);

        // Assert
        Assert.Equal(0, index1.Index);
        Assert.Equal(1, index2.Index);
        Assert.Equal(2, index3.Index);
        Assert.Equal(60, _metaDb.NextCursor.ToTotalBytes());
    }

    [Fact]
    public void Get_AfterAppend_ReturnsCorrectData()
    {
        // Arrange
        var originalIndex = _metaDb.Append(
            ElementTokenType.Number,
            location: 123,
            sizeOrLength: 456,
            sourceDocumentId: 7,
            parentRow: 89,
            operationReferenceId: 12,
            flags: ElementFlags.IsNullable | ElementFlags.IsExcluded);

        // Act
        var row = _metaDb.Get(originalIndex);

        // Assert
        Assert.Equal(ElementTokenType.Number, row.TokenType);
        Assert.Equal(123, row.Location);
        Assert.Equal(456, row.SizeOrLength);
        Assert.Equal(7, row.SourceDocumentId);
        Assert.Equal(89, row.ParentRow);
        Assert.Equal(12, row.OperationReferenceId);
        Assert.Equal(ElementFlags.IsNullable | ElementFlags.IsExcluded, row.Flags);
        Assert.False(row.HasComplexChildren);
    }

    [Fact]
    public void Get_WithComplexChildren_ReturnsCorrectFlag()
    {
        // Arrange
        var index = _metaDb.Append(
            ElementTokenType.String,
            sizeOrLength: -1); // Negative sets HasComplexChildren

        // Act
        var row = _metaDb.Get(index);

        // Assert
        Assert.True(row.HasComplexChildren);
        Assert.Equal(int.MaxValue, row.SizeOrLength); // Should mask off sign bit
    }

    [Fact]
    public void Replace_ExistingRow_UpdatesCorrectly()
    {
        // Arrange
        var index = _metaDb.Append(ElementTokenType.String, location: 10);

        // Act
        _metaDb.Replace(
            index,
            ElementTokenType.Number,
            location: 999,
            sizeOrLength: 888,
            sourceDocumentId: 3);

        // Assert
        var row = _metaDb.Get(index);
        Assert.Equal(ElementTokenType.Number, row.TokenType);
        Assert.Equal(999, row.Location);
        Assert.Equal(888, row.SizeOrLength);
        Assert.Equal(3, row.SourceDocumentId);
    }

    [Fact]
    public void GetElementTokenType_AfterAppend_ReturnsCorrectType()
    {
        // Arrange
        var index = _metaDb.Append(ElementTokenType.StartArray);

        // Act
        var tokenType = _metaDb.GetElementTokenType(index);

        // Assert
        Assert.Equal(ElementTokenType.StartArray, tokenType);
    }

    [Fact]
    public void Append_ManyRows_HandlesChunkBoundaries()
    {
        // Arrange - Calculate how many rows fit in a chunk
        const int chunkSize = 128 * 1024; // 128KB
        const int rowsPerChunk = chunkSize / 20; // DbRow.Size = 20

        // Act - Add more rows than fit in a single chunk
        var indices = new List<Cursor>();
        for (var i = 0; i < rowsPerChunk + 10; i++)
        {
            indices.Add(_metaDb.Append(ElementTokenType.String, location: i));
        }

        // Assert
        Assert.Equal(rowsPerChunk + 10, indices.Count);

        // Verify we can read from both chunks
        var firstRow = _metaDb.Get(Cursor.FromIndex(0));
        var lastRow = _metaDb.Get(indices.Last());

        Assert.Equal(0, firstRow.Location);
        Assert.Equal(rowsPerChunk + 9, lastRow.Location);
    }

    [Fact]
    public void Append_WithMaxValues_StoresCorrectly()
    {
        // Arrange - Test boundary values
        const int maxLocation = 0x07FFFFFF; // 27 bits
        const int maxSizeOrLength = int.MaxValue; // 31 bits
        const int maxSourceDocumentId = 0x7FFF; // 15 bits (reduced from 16)
        const int maxParentRow = 0x0FFFFFFF; // 28 bits
        const int maxSelectionSetId = 0x7FFF; // 15 bits

        // Act
        var index = _metaDb.Append(
            ElementTokenType.Reference,
            location: maxLocation,
            sizeOrLength: maxSizeOrLength,
            sourceDocumentId: maxSourceDocumentId,
            parentRow: maxParentRow,
            operationReferenceId: maxSelectionSetId,
            flags: ElementFlags.IsRoot | ElementFlags.IsNullable | ElementFlags.IsExcluded);

        // Assert
        var row = _metaDb.Get(index);
        Assert.Equal(ElementTokenType.Reference, row.TokenType);
        Assert.Equal(maxLocation, row.Location);
        Assert.Equal(maxSizeOrLength, row.SizeOrLength);
        Assert.Equal(maxSourceDocumentId, row.SourceDocumentId);
        Assert.Equal(maxParentRow, row.ParentRow);
        Assert.Equal(maxSelectionSetId, row.OperationReferenceId);
    }

    [Fact]
    public void IsSimpleValue_ForPrimitiveTypes_ReturnsTrue()
    {
        // Arrange & Act
        var stringIndex = _metaDb.Append(ElementTokenType.String);
        var numberIndex = _metaDb.Append(ElementTokenType.Number);
        var boolIndex = _metaDb.Append(ElementTokenType.True);
        var nullIndex = _metaDb.Append(ElementTokenType.Null);
        var propIndex = _metaDb.Append(ElementTokenType.PropertyName);

        // Assert
        Assert.True(_metaDb.Get(stringIndex).IsSimpleValue);
        Assert.True(_metaDb.Get(numberIndex).IsSimpleValue);
        Assert.True(_metaDb.Get(boolIndex).IsSimpleValue);
        Assert.True(_metaDb.Get(nullIndex).IsSimpleValue);
        Assert.True(_metaDb.Get(propIndex).IsSimpleValue);
    }

    [Fact]
    public void IsSimpleValue_ForComplexTypes_ReturnsFalse()
    {
        // Arrange & Act
        var objectIndex = _metaDb.Append(ElementTokenType.StartObject);
        var arrayIndex = _metaDb.Append(ElementTokenType.StartArray);
        var refIndex = _metaDb.Append(ElementTokenType.Reference);

        // Assert
        Assert.False(_metaDb.Get(objectIndex).IsSimpleValue);
        Assert.False(_metaDb.Get(arrayIndex).IsSimpleValue);
        Assert.False(_metaDb.Get(refIndex).IsSimpleValue);
    }

    [Fact]
    public void IsUnknownSize_WhenSizeIsUnknown_ReturnsTrue()
    {
        // Arrange
        var index = _metaDb.Append(
            ElementTokenType.String,
            sizeOrLength: DbRow.UnknownSize);

        // Act & Assert
        var row = _metaDb.Get(index);
        Assert.True(row.IsUnknownSize);
    }

    [Theory]
    [InlineData((int)ElementTokenType.String, 100, 50)]
    [InlineData((int)ElementTokenType.Number, 200, 25)]
    [InlineData((int)ElementTokenType.True, 0, 0)]
    public void Append_VariousTokenTypes_StoresCorrectly(
        int tokenType,
        int location,
        int sizeOrLength)
    {
        // Arrange & Act
        var index = _metaDb.Append((ElementTokenType)tokenType, location, sizeOrLength);

        // Assert
        var row = _metaDb.Get(index);
        Assert.Equal((ElementTokenType)tokenType, row.TokenType);
        Assert.Equal(location, row.Location);
        Assert.Equal(sizeOrLength, row.SizeOrLength);
    }

    [Fact]
    public void Dispose_WhenCalled_CleansUpResources()
    {
        // Arrange
        var metaDb = MetaDb.CreateForEstimatedRows(10);
        metaDb.Append(ElementTokenType.String);

        // Act & Assert - Should not throw
        metaDb.Dispose();
    }

    [Fact]
    public void MultipleDispose_DoesNotThrow()
    {
        // Arrange
        var metaDb = MetaDb.CreateForEstimatedRows(10);

        // Act & Assert - Should not throw
        metaDb.Dispose();
        metaDb.Dispose();
    }

    [Fact]
    public void Append_ExceedsInitialChunkCapacity_ExpandsChunkArray()
    {
        // Arrange
        using var metaDb = MetaDb.CreateForEstimatedRows(4);

        const int chunkSize = 128 * 1024;
        const int rowsPerChunk = chunkSize / 20;
        const int totalRowsToAdd = (rowsPerChunk * 4) + 10;

        // Act - Add enough rows to exceed initial capacity and trigger expansion
        var indices = new List<Cursor>();
        for (var i = 0; i < totalRowsToAdd; i++)
        {
            indices.Add(metaDb.Append(ElementTokenType.String, location: i, sizeOrLength: i % 100));
        }

        // Assert
        Assert.Equal(totalRowsToAdd, indices.Count);

        // since 20 bytes do not fit perfectly into 128kb buffers we have some
        // extra skip bytes.
        Assert.Equal((totalRowsToAdd * 20) + 48, metaDb.NextCursor.ToTotalBytes());

        // Verify we can read data from all chunks
        var firstRow = metaDb.Get(Cursor.FromIndex(0));
        var middleRow = metaDb.Get(Cursor.FromIndex(totalRowsToAdd / 2));
        var lastRow = metaDb.Get(Cursor.FromIndex(totalRowsToAdd - 1));

        Assert.Equal(0, firstRow.Location);
        Assert.Equal(totalRowsToAdd / 2, middleRow.Location);
        Assert.Equal(totalRowsToAdd - 1, lastRow.Location);

        // Verify size values (using modulo from the loop)
        Assert.Equal(0, firstRow.SizeOrLength);
        Assert.Equal((totalRowsToAdd / 2) % 100, middleRow.SizeOrLength);
        Assert.Equal((totalRowsToAdd - 1) % 100, lastRow.SizeOrLength);
    }

    [Fact]
    public void Append_StoresAndReadsNumberOfRows()
    {
        // Arrange & Act
        var index = _metaDb.Append(
            ElementTokenType.StartObject,
            sizeOrLength: 3,
            parentRow: 10,
            numberOfRows: 7);

        // Assert
        var row = _metaDb.Get(index);
        Assert.Equal(ElementTokenType.StartObject, row.TokenType);
        Assert.Equal(3, row.SizeOrLength);
        Assert.Equal(10, row.ParentRow);
        Assert.Equal(7, row.NumberOfRows);
    }

    [Fact]
    public void Append_WithMaxNumberOfRows_StoresCorrectly()
    {
        // Arrange
        const int maxNumberOfRows = 0x07FFFFFF; // 27 bits

        // Act
        var index = _metaDb.Append(
            ElementTokenType.StartArray,
            numberOfRows: maxNumberOfRows);

        // Assert
        var row = _metaDb.Get(index);
        Assert.Equal(maxNumberOfRows, row.NumberOfRows);
        Assert.Equal(ElementTokenType.StartArray, row.TokenType);
    }

    [Fact]
    public void SetNumberOfRows_UpdatesValueWithoutAffectingOtherFields()
    {
        // Arrange
        var index = _metaDb.Append(
            ElementTokenType.StartObject,
            sizeOrLength: 5,
            parentRow: 100,
            operationReferenceId: 42,
            flags: ElementFlags.IsRoot);

        // Act
        _metaDb.SetNumberOfRows(index, 11);

        // Assert — NumberOfRows updated, other fields preserved
        var row = _metaDb.Get(index);
        Assert.Equal(11, row.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, row.TokenType);
        Assert.Equal(5, row.SizeOrLength);
        Assert.Equal(100, row.ParentRow);
        Assert.Equal(42, row.OperationReferenceId);
        Assert.Equal(ElementFlags.IsRoot, row.Flags);
    }

    [Fact]
    public void SetFlags_UpdatesValueWithoutAffectingOtherFields()
    {
        // Arrange
        var index = _metaDb.Append(
            ElementTokenType.PropertyName,
            parentRow: 100,
            operationReferenceId: 42,
            operationReferenceType: OperationReferenceType.Selection,
            flags: ElementFlags.None);

        // Act
        _metaDb.SetFlags(index, ElementFlags.IsNullable | ElementFlags.IsRoot);

        // Assert — Flags updated, other fields preserved
        var row = _metaDb.Get(index);
        Assert.Equal(ElementFlags.IsNullable | ElementFlags.IsRoot, row.Flags);
        Assert.Equal(ElementTokenType.PropertyName, row.TokenType);
        Assert.Equal(100, row.ParentRow);
        Assert.Equal(42, row.OperationReferenceId);
        Assert.Equal(OperationReferenceType.Selection, row.OperationReferenceType);
    }

    [Fact]
    public void SetSizeOrLength_UpdatesValueWithoutAffectingOtherFields()
    {
        // Arrange
        var index = _metaDb.Append(
            ElementTokenType.StartObject,
            sizeOrLength: 0,
            parentRow: 7,
            numberOfRows: 9);

        // Act
        _metaDb.SetSizeOrLength(index, 15);

        // Assert
        var row = _metaDb.Get(index);
        Assert.Equal(15, row.SizeOrLength);
        Assert.Equal(ElementTokenType.StartObject, row.TokenType);
        Assert.Equal(7, row.ParentRow);
        Assert.Equal(9, row.NumberOfRows);
    }

    [Fact]
    public void AppendNull_WritesTokenTypeNoneAndParent()
    {
        // Act
        var cursor = _metaDb.AppendNull(parentRow: 42);

        // Assert
        var row = _metaDb.Get(cursor);
        Assert.Equal(ElementTokenType.None, row.TokenType);
        Assert.Equal(42, row.ParentRow);
        Assert.Equal(0, row.Location);
        Assert.Equal(0, row.SizeOrLength);
        Assert.Equal(0, row.SourceDocumentId);
        Assert.Equal(0, row.OperationReferenceId);
        Assert.Equal(ElementFlags.None, row.Flags);
        Assert.Equal(OperationReferenceType.None, row.OperationReferenceType);
    }

    [Fact]
    public void AppendNull_AdvancesCursor()
    {
        // Act
        var c0 = _metaDb.AppendNull(0);
        var c1 = _metaDb.AppendNull(c0.Index);
        var c2 = _metaDb.AppendNull(c1.Index);

        // Assert
        Assert.Equal(0, c0.Index);
        Assert.Equal(1, c1.Index);
        Assert.Equal(2, c2.Index);
        Assert.Equal(0, _metaDb.Get(c0).ParentRow);
        Assert.Equal(c0.Index, _metaDb.Get(c1).ParentRow);
        Assert.Equal(c1.Index, _metaDb.Get(c2).ParentRow);
    }

    [Fact]
    public void AppendNull_IsEquivalentToGenericAppend()
    {
        // Arrange — compare specialized vs generic path
        using var reference = MetaDb.CreateForEstimatedRows(10);
        var refCursor = reference.Append(ElementTokenType.None, parentRow: 123);

        // Act
        var cursor = _metaDb.AppendNull(parentRow: 123);

        // Assert — rows must be byte-for-byte identical
        var refRow = reference.Get(refCursor);
        var row = _metaDb.Get(cursor);

        Assert.Equal(refRow.TokenType, row.TokenType);
        Assert.Equal(refRow.ParentRow, row.ParentRow);
        Assert.Equal(refRow.Location, row.Location);
        Assert.Equal(refRow.SizeOrLength, row.SizeOrLength);
        Assert.Equal(refRow.NumberOfRows, row.NumberOfRows);
        Assert.Equal(refRow.SourceDocumentId, row.SourceDocumentId);
        Assert.Equal(refRow.OperationReferenceId, row.OperationReferenceId);
        Assert.Equal(refRow.OperationReferenceType, row.OperationReferenceType);
        Assert.Equal(refRow.Flags, row.Flags);
    }

    [Fact]
    public void AppendEmptyProperty_WritesAllFields()
    {
        // Act
        var cursor = _metaDb.AppendEmptyProperty(
            parentRow: 7,
            selectionId: 99,
            flags: ElementFlags.IsNullable | ElementFlags.IsInternal);

        // Assert
        var row = _metaDb.Get(cursor);
        Assert.Equal(ElementTokenType.PropertyName, row.TokenType);
        Assert.Equal(7, row.ParentRow);
        Assert.Equal(99, row.OperationReferenceId);
        Assert.Equal(OperationReferenceType.Selection, row.OperationReferenceType);
        Assert.Equal(ElementFlags.IsNullable | ElementFlags.IsInternal, row.Flags);
        Assert.Equal(0, row.Location);
        Assert.Equal(0, row.SizeOrLength);
        Assert.Equal(0, row.NumberOfRows);
        Assert.Equal(0, row.SourceDocumentId);
    }

    [Fact]
    public void AppendEmptyProperty_WithNoFlags()
    {
        // Act
        var cursor = _metaDb.AppendEmptyProperty(
            parentRow: 0,
            selectionId: 1,
            flags: ElementFlags.None);

        // Assert
        var row = _metaDb.Get(cursor);
        Assert.Equal(ElementTokenType.PropertyName, row.TokenType);
        Assert.Equal(0, row.ParentRow);
        Assert.Equal(1, row.OperationReferenceId);
        Assert.Equal(ElementFlags.None, row.Flags);
    }

    [Fact]
    public void AppendEmptyProperty_IsEquivalentToGenericAppend()
    {
        // Arrange
        using var reference = MetaDb.CreateForEstimatedRows(10);
        var refCursor = reference.Append(
            ElementTokenType.PropertyName,
            parentRow: 13,
            operationReferenceId: 77,
            operationReferenceType: OperationReferenceType.Selection,
            flags: ElementFlags.IsExcluded);

        // Act
        var cursor = _metaDb.AppendEmptyProperty(
            parentRow: 13,
            selectionId: 77,
            flags: ElementFlags.IsExcluded);

        // Assert
        var refRow = reference.Get(refCursor);
        var row = _metaDb.Get(cursor);

        Assert.Equal(refRow.TokenType, row.TokenType);
        Assert.Equal(refRow.ParentRow, row.ParentRow);
        Assert.Equal(refRow.OperationReferenceId, row.OperationReferenceId);
        Assert.Equal(refRow.OperationReferenceType, row.OperationReferenceType);
        Assert.Equal(refRow.Flags, row.Flags);
        Assert.Equal(refRow.Location, row.Location);
        Assert.Equal(refRow.SizeOrLength, row.SizeOrLength);
        Assert.Equal(refRow.NumberOfRows, row.NumberOfRows);
    }

    [Fact]
    public void AppendEmptyPropertyWithNullValue_WritesTwoLinkedRows()
    {
        // Act
        var propCursor = _metaDb.AppendEmptyPropertyWithNullValue(
            parentRow: 5,
            selectionId: 11,
            flags: ElementFlags.IsNullable);

        // Assert — PropertyName row
        var propRow = _metaDb.Get(propCursor);
        Assert.Equal(ElementTokenType.PropertyName, propRow.TokenType);
        Assert.Equal(5, propRow.ParentRow);
        Assert.Equal(11, propRow.OperationReferenceId);
        Assert.Equal(OperationReferenceType.Selection, propRow.OperationReferenceType);
        Assert.Equal(ElementFlags.IsNullable, propRow.Flags);

        // Assert — None value row with parent = PropertyName cursor
        var valueCursor = Cursor.FromIndex(propCursor.Index + 1);
        var valueRow = _metaDb.Get(valueCursor);
        Assert.Equal(ElementTokenType.None, valueRow.TokenType);
        Assert.Equal(propCursor.Index, valueRow.ParentRow);
        Assert.Equal(0, valueRow.Location);
        Assert.Equal(0, valueRow.OperationReferenceId);
        Assert.Equal(ElementFlags.None, valueRow.Flags);

        // Cursor advanced by 2
        Assert.Equal(propCursor.Index + 2, _metaDb.NextCursor.Index);
    }

    [Fact]
    public void AppendEmptyPropertyWithNullValue_IsEquivalentToTwoGenericAppends()
    {
        // Arrange
        using var reference = MetaDb.CreateForEstimatedRows(10);
        var refProp = reference.Append(
            ElementTokenType.PropertyName,
            parentRow: 21,
            operationReferenceId: 3,
            operationReferenceType: OperationReferenceType.Selection,
            flags: ElementFlags.IsInternal);
        var refNull = reference.Append(
            ElementTokenType.None,
            parentRow: refProp.Index);

        // Act
        var propCursor = _metaDb.AppendEmptyPropertyWithNullValue(
            parentRow: 21,
            selectionId: 3,
            flags: ElementFlags.IsInternal);
        var valueCursor = Cursor.FromIndex(propCursor.Index + 1);

        // Assert property rows match byte-for-byte
        var refPropRow = reference.Get(refProp);
        var propRow = _metaDb.Get(propCursor);
        Assert.Equal(refPropRow.TokenType, propRow.TokenType);
        Assert.Equal(refPropRow.ParentRow, propRow.ParentRow);
        Assert.Equal(refPropRow.OperationReferenceId, propRow.OperationReferenceId);
        Assert.Equal(refPropRow.OperationReferenceType, propRow.OperationReferenceType);
        Assert.Equal(refPropRow.Flags, propRow.Flags);

        // Assert value rows match byte-for-byte
        var refNullRow = reference.Get(refNull);
        var valueRow = _metaDb.Get(valueCursor);
        Assert.Equal(refNullRow.TokenType, valueRow.TokenType);
        Assert.Equal(refNullRow.ParentRow, valueRow.ParentRow);
    }

    [Fact]
    public void AppendEmptyPropertyWithNullValue_FallsBackAcrossChunkBoundary()
    {
        // Arrange — fill current chunk so only 1 row fits, forcing slow path.
        using var metaDb = MetaDb.CreateForEstimatedRows(4);
        const int rowsPerChunk = 128 * 1024 / 20;

        // Fill all but one slot in the first chunk
        for (var i = 0; i < rowsPerChunk - 1; i++)
        {
            metaDb.AppendNull(0);
        }

        // Act — pair cannot fit, so the slow path runs (single-row Append + AppendNull).
        var propCursor = metaDb.AppendEmptyPropertyWithNullValue(
            parentRow: 0,
            selectionId: 7,
            flags: ElementFlags.None);
        var valueCursor = Cursor.FromIndex(propCursor.Index + 1);

        // Assert — correctness preserved across boundary
        var propRow = metaDb.Get(propCursor);
        var valueRow = metaDb.Get(valueCursor);
        Assert.Equal(ElementTokenType.PropertyName, propRow.TokenType);
        Assert.Equal(7, propRow.OperationReferenceId);
        Assert.Equal(ElementTokenType.None, valueRow.TokenType);
        Assert.Equal(propCursor.Index, valueRow.ParentRow);
    }

    [Fact]
    public void AppendNull_FollowsChunkBoundary()
    {
        // Arrange — fill the first chunk and verify AppendNull keeps advancing into chunk 2.
        using var metaDb = MetaDb.CreateForEstimatedRows(4);
        const int rowsPerChunk = 128 * 1024 / 20;

        for (var i = 0; i < rowsPerChunk + 5; i++)
        {
            metaDb.AppendNull(i);
        }

        // Assert — every row readable with expected parent
        for (var i = 0; i < rowsPerChunk + 5; i++)
        {
            var row = metaDb.Get(Cursor.FromIndex(i));
            Assert.Equal(ElementTokenType.None, row.TokenType);
            Assert.Equal(i, row.ParentRow);
        }
    }

    public void Dispose() => _metaDb.Dispose();
}
