namespace HotChocolate.Fusion.Text.Json;

public class CompositeResultDocumentMetaDbTests : IDisposable
{
    private CompositeResultDocument.MetaDb _metaDb;

    public CompositeResultDocumentMetaDbTests()
    {
        _metaDb = CompositeResultDocument.MetaDb.CreateForEstimatedRows(100);
    }

    public void Dispose()
    {
        _metaDb.Dispose();
    }

    [Fact]
    public void CreateForEstimatedRows_WithSmallEstimate_CreatesValidMetaDb()
    {
        // Arrange & Act
        using var metaDb = CompositeResultDocument.MetaDb.CreateForEstimatedRows(10);

        // Assert
        Assert.Equal(0, metaDb.Length);
    }

    [Fact]
    public void Append_SingleRow_ReturnsCorrectIndex()
    {
        // Arrange & Act
        var index = _metaDb.Append(
            ElementTokenType.String,
            location: 42,
            sizeOrLength: 10,
            sourceDocumentId: 1,
            parentRow: 0,
            selectionSetId: 5,
            flags: ElementFlags.None);

        // Assert
        Assert.Equal(0, index);
        Assert.Equal(20, _metaDb.Length); // DbRow.Size = 20
    }

    [Fact]
    public void Append_MultipleRows_ReturnsSequentialIndices()
    {
        // Arrange & Act
        var index1 = _metaDb.Append(ElementTokenType.StartObject);
        var index2 = _metaDb.Append(ElementTokenType.String, location: 10);
        var index3 = _metaDb.Append(ElementTokenType.EndObject);

        // Assert
        Assert.Equal(0, index1);
        Assert.Equal(1, index2);
        Assert.Equal(2, index3);
        Assert.Equal(60, _metaDb.Length); // 3 * DbRow.Size
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
            selectionSetId: 12,
            flags: ElementFlags.IsNullable | ElementFlags.IsLeaf);

        // Act
        var row = _metaDb.Get(originalIndex);

        // Assert
        Assert.Equal(ElementTokenType.Number, row.TokenType);
        Assert.Equal(123, row.Location);
        Assert.Equal(456, row.SizeOrLength);
        Assert.Equal(7, row.SourceDocumentId);
        Assert.Equal(89, row.ParentRow);
        Assert.Equal(12, row.SelectionSetId);
        Assert.Equal(ElementFlags.IsNullable | ElementFlags.IsLeaf, row.Flags);
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
        var indices = new List<int>();
        for (int i = 0; i < rowsPerChunk + 10; i++)
        {
            indices.Add(_metaDb.Append(ElementTokenType.String, location: i));
        }

        // Assert
        Assert.Equal(rowsPerChunk + 10, indices.Count);

        // Verify we can read from both chunks
        var firstRow = _metaDb.Get(0);
        var lastRow = _metaDb.Get(indices.Last());

        Assert.Equal(0, firstRow.Location);
        Assert.Equal(rowsPerChunk + 9, lastRow.Location);
    }

    [Fact]
    public void Append_WithMaxValues_StoresCorrectly()
    {
        // Arrange - Test boundary values
        const int maxLocation = 0x0FFFFFFF; // 28 bits
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
            selectionSetId: maxSelectionSetId,
            flags: ElementFlags.IsRoot | ElementFlags.IsNullable | ElementFlags.IsLeaf);

        // Assert
        var row = _metaDb.Get(index);
        Assert.Equal(ElementTokenType.Reference, row.TokenType);
        Assert.Equal(maxLocation, row.Location);
        Assert.Equal(maxSizeOrLength, row.SizeOrLength);
        Assert.Equal(maxSourceDocumentId, row.SourceDocumentId);
        Assert.Equal(maxParentRow, row.ParentRow);
        Assert.Equal(maxSelectionSetId, row.SelectionSetId);
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
            sizeOrLength: CompositeResultDocument.DbRow.UnknownSize);

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
        var metaDb = CompositeResultDocument.MetaDb.CreateForEstimatedRows(10);
        metaDb.Append(ElementTokenType.String);

        // Act & Assert - Should not throw
        metaDb.Dispose();
    }

    [Fact]
    public void MultipleDispose_DoesNotThrow()
    {
        // Arrange
        var metaDb = CompositeResultDocument.MetaDb.CreateForEstimatedRows(10);

        // Act & Assert - Should not throw
        metaDb.Dispose();
        metaDb.Dispose();
    }

    [Fact]
    public void Append_ExceedsInitialChunkCapacity_ExpandsChunkArray()
    {
        // Arrange - Start with minimal estimate to force expansion
        using var metaDb = CompositeResultDocument.MetaDb.CreateForEstimatedRows(1);

        const int chunkSize = 128 * 1024; // 128KB
        const int rowsPerChunk = chunkSize / 20; // DbRow.Size = 20
        const int totalRowsToAdd = (rowsPerChunk * 4) + 10; // Force multiple chunk allocations

        // Act - Add enough rows to exceed initial capacity and trigger expansion
        var indices = new List<int>();
        for (int i = 0; i < totalRowsToAdd; i++)
        {
            indices.Add(metaDb.Append(ElementTokenType.String, location: i, sizeOrLength: i % 100));
        }

        // Assert - Verify all rows were added correctly
        Assert.Equal(totalRowsToAdd, indices.Count);
        Assert.Equal(totalRowsToAdd * 20, metaDb.Length);

        // Verify we can read data from all chunks
        var firstRow = metaDb.Get(0);
        var middleRow = metaDb.Get(totalRowsToAdd / 2);
        var lastRow = metaDb.Get(totalRowsToAdd - 1);

        Assert.Equal(0, firstRow.Location);
        Assert.Equal(totalRowsToAdd / 2, middleRow.Location);
        Assert.Equal(totalRowsToAdd - 1, lastRow.Location);

        // Verify size values (using modulo from the loop)
        Assert.Equal(0, firstRow.SizeOrLength);
        Assert.Equal((totalRowsToAdd / 2) % 100, middleRow.SizeOrLength);
        Assert.Equal((totalRowsToAdd - 1) % 100, lastRow.SizeOrLength);
    }
}
