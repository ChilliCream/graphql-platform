using System.Text.Json;

namespace HotChocolate.Fusion.SourceSchema.Packaging;

public class FusionSourceSchemaArchiveTests : IDisposable
{
    private readonly List<Stream> _streamsToDispose = [];

    [Fact]
    public void Create_WithNullStream_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => FusionSourceSchemaArchive.Create(null!));
    }

    [Fact]
    public void Open_WithNullStream_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => FusionSourceSchemaArchive.Open(default(Stream)!));
    }

    [Fact]
    public void Open_WithNullString_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => FusionSourceSchemaArchive.Open(default(string)!));
    }

    [Fact]
    public async Task SetArchiveMetadata_WithValidData_StoresCorrectly()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            FormatVersion = new Version("2.0.0")
        };

        // act & Assert
        using var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);

        // Can read immediately within the same session
        var retrieved = await archive.GetArchiveMetadataAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(metadata.FormatVersion, retrieved.FormatVersion);
    }

    [Fact]
    public async Task GetArchiveMetadata_WhenNotSet_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = FusionSourceSchemaArchive.Create(stream);
        var result = await archive.GetArchiveMetadataAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetArchiveMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = FusionSourceSchemaArchive.Create(stream);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.SetArchiveMetadataAsync(null!));
    }

    [Fact]
    public async Task SetSchema_WithValidData_StoresCorrectly()
    {
        // arrange
        await using var stream = CreateStream();
        var schema = "type Query { hello: String }"u8.ToArray();

        // act
        using var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true);
        await archive.SetSchemaAsync(schema);

        // assert - Can read immediately within the same session
        var retrieved = await archive.TryGetSchemaAsync();

        Assert.NotNull(retrieved);
        Assert.Equal(schema, retrieved.Value.ToArray());
    }

    [Fact]
    public async Task SetSchema_WithEmptySchema_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        await using var stream = CreateStream();
        var schema = ReadOnlyMemory<byte>.Empty;

        // act & assert
        using var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.SetSchemaAsync(schema));
    }

    [Fact]
    public async Task TryGetSchema_WhenNotSet_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act
        using var archive = FusionSourceSchemaArchive.Create(stream);
        var result = await archive.TryGetSchemaAsync();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetSettings_WithValidData_StoresCorrectly()
    {
        // arrange
        await using var stream = CreateStream();
        using var settings = JsonDocument.Parse("""{"version": "1.0", "name": "test"}""");

        // act
        using var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true);
        await archive.SetSettingsAsync(settings);

        // assert - Can read immediately within the same session
        using var retrieved = await archive.TryGetSettingsAsync();

        Assert.NotNull(retrieved);
        Assert.Equal("1.0", retrieved.RootElement.GetProperty("version").GetString());
        Assert.Equal("test", retrieved.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task SetSettings_WithNullSettings_ThrowsArgumentNullException()
    {
        // arrange
        await using var stream = CreateStream();

        // act & assert
        using var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.SetSettingsAsync(null!));
    }

    [Fact]
    public async Task TryGetSettings_WhenNotSet_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act
        using var archive = FusionSourceSchemaArchive.Create(stream);
        var result = await archive.TryGetSettingsAsync();

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CommitAsync_WhenSchemaNotSet_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata();
        using var settings = JsonDocument.Parse("""{"version": "1.0", "name": "test"}""");

        // act & assert
        using var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetSettingsAsync(settings);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.CommitAsync());
        Assert.Equal(
            "Cannot commit changes as long as one of the following has not been set: GraphQL schema, settings or archive metadata.",
            exception.Message);
    }

    [Fact]
    public async Task CommitAsync_WhenSettingsNotSet_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata();
        var schema = "type Query { hello: String }"u8.ToArray();

        // act & assert
        using var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetSchemaAsync(schema);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.CommitAsync());
        Assert.Equal(
            "Cannot commit changes as long as one of the following has not been set: GraphQL schema, settings or archive metadata.",
            exception.Message);
    }

    [Fact]
    public async Task CommitAsync_WhenArchiveMetadataNotSet_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var schema = "type Query { hello: String }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"version": "1.0", "name": "test"}""");

        // act & assert
        using var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true);
        await archive.SetSchemaAsync(schema);
        await archive.SetSettingsAsync(settings);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.CommitAsync());
        Assert.Equal(
            "Cannot commit changes as long as one of the following has not been set: GraphQL schema, settings or archive metadata.",
            exception.Message);
    }

    [Fact]
    public async Task CommitAsync_WhenReadOnly_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata();
        var schema = "type Query { hello: String }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"version": "1.0", "name": "test"}""");

        // Create and commit a valid archive first
        using (var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.SetSchemaAsync(schema);
            await archive.SetSettingsAsync(settings);
            await archive.CommitAsync();
        }

        // act & assert - Open in read mode and try to commit
        stream.Position = 0;
        using (var readArchive = FusionSourceSchemaArchive.Open(stream, FusionSourceSchemaArchiveMode.Read, leaveOpen: true))
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => readArchive.CommitAsync());
            Assert.Equal("Cannot commit changes to a read-only archive.", exception.Message);
        }
    }

    [Fact]
    public async Task CommitAndReopen_PersistsAllData()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata();
        var schema = "type Query { users: [User] } type User { id: ID! name: String! }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"version": "1.0", "name": "users"}""");

        // act - Create and commit
        using (var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.SetSchemaAsync(schema);
            await archive.SetSettingsAsync(settings);
            await archive.CommitAsync();
        }

        // assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = FusionSourceSchemaArchive.Open(stream, leaveOpen: true))
        {
            var retrievedMetadata = await readArchive.GetArchiveMetadataAsync();
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(metadata.FormatVersion, retrievedMetadata.FormatVersion);

            var retrievedSchema = await readArchive.TryGetSchemaAsync();
            Assert.NotNull(retrievedSchema);
            Assert.Equal(schema, retrievedSchema.Value.ToArray());

            using var retrievedSettings = await readArchive.TryGetSettingsAsync();
            Assert.NotNull(retrievedSettings);
            Assert.Equal("1.0", retrievedSettings.RootElement.GetProperty("version").GetString());
            Assert.Equal("users", retrievedSettings.RootElement.GetProperty("name").GetString());
        }
    }

    [Fact]
    public async Task UpdateMode_CanModifyExistingArchive()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata();
        var schema1 = "type Query { hello: String }"u8.ToArray();
        var schema2 = "type Query { hello: String! goodbye: String }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"version": "1.0", "name": "test"}""");

        // act - Create initial archive
        using (var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.SetSchemaAsync(schema1);
            await archive.SetSettingsAsync(settings);
            await archive.CommitAsync();
        }

        // act - Update existing archive with new schema
        stream.Position = 0;
        using (var updateArchive = FusionSourceSchemaArchive.Open(stream, FusionSourceSchemaArchiveMode.Update, leaveOpen: true))
        {
            await updateArchive.SetSchemaAsync(schema2);
            await updateArchive.CommitAsync();
        }

        // assert - Verify updated schema
        stream.Position = 0;
        using (var readArchive = FusionSourceSchemaArchive.Open(stream, leaveOpen: true))
        {
            var retrievedSchema = await readArchive.TryGetSchemaAsync();
            Assert.NotNull(retrievedSchema);
            Assert.Equal(schema2, retrievedSchema.Value.ToArray());
        }
    }

    private Stream CreateStream()
    {
        var stream = new MemoryStream();
        _streamsToDispose.Add(stream);
        return stream;
    }

    public void Dispose()
    {
        foreach (var stream in _streamsToDispose)
        {
            stream.Dispose();
        }
    }
}
