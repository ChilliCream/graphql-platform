using System.Text;
using System.Text.Json;

namespace HotChocolate.Adapters.OpenApi.Packaging;

public class OpenApiCollectionArchiveTests : IDisposable
{
    private readonly List<Stream> _streamsToDispose = [];

    [Fact]
    public void Create_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenApiCollectionArchive.Create(null!));
    }

    [Fact]
    public void Open_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenApiCollectionArchive.Open(default(Stream)!));
    }

    [Fact]
    public void Open_WithNullString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenApiCollectionArchive.Open(default(string)!));
    }

    [Fact]
    public async Task SetArchiveMetadata_WithValidData_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            FormatVersion = new Version("2.0.0")
        };

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);

        // Can read immediately within the same session
        var retrieved = await archive.GetArchiveMetadataAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(metadata.FormatVersion, retrieved.FormatVersion);
    }

    [Fact]
    public async Task GetArchiveMetadata_WhenNotSet_ReturnsNull()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        var result = await archive.GetArchiveMetadataAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetArchiveMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.SetArchiveMetadataAsync(null!));
    }

    // [Fact]
    // public async Task GetLatestSupportedGatewayFormat_WithValidMetadata_ReturnsHighestVersion()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.1.0"), new Version("2.0.0")],
    //         SourceSchemas = ["test-service"]
    //     };
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetArchiveMetadataAsync(metadata);
    //     var latest = await archive.GetLatestSupportedGatewayFormatAsync();
    //     Assert.Equal(new Version("2.1.0"), latest);
    // }
    //
    // [Fact]
    // public async Task GetLatestSupportedGatewayFormat_WithoutMetadata_ThrowsInvalidOperationException()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream);
    //     await Assert.ThrowsAsync<InvalidOperationException>(
    //         () => archive.GetLatestSupportedGatewayFormatAsync());
    // }
    //
    // [Fact]
    // public async Task SetCompositionSettings_WithValidJsonDocument_StoresCorrectly()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     const string settingsJson = """{"enableNodeSpec": true, "maxDepth": 10}""";
    //     using var settings = JsonDocument.Parse(settingsJson);
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetCompositionSettingsAsync(settings);
    //
    //     // Can read immediately within the same session
    //     using var retrieved = await archive.GetCompositionSettingsAsync();
    //     Assert.NotNull(retrieved);
    //     Assert.True(retrieved.RootElement.GetProperty("enableNodeSpec").GetBoolean());
    //     Assert.Equal(10, retrieved.RootElement.GetProperty("maxDepth").GetInt32());
    // }
    //
    // [Fact]
    // public async Task GetCompositionSettings_WhenNotSet_ReturnsNull()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream);
    //     var result = await archive.GetCompositionSettingsAsync();
    //     Assert.Null(result);
    // }
    //
    // [Fact]
    // public async Task SetGatewaySchema_WithStringContent_StoresCorrectly()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     const string schema = "type Query { hello: String }";
    //     var settings = CreateSettingsJson();
    //     var version = new Version("2.0.0");
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     var metadata = CreateTestMetadata();
    //     await archive.SetArchiveMetadataAsync(metadata);
    //     await archive.SetGatewayConfigurationAsync(schema, settings, version);
    //
    //     // Can read immediately within the same session
    //     var result = await archive.TryGetGatewayConfigurationAsync(version);
    //
    //     Assert.NotNull(result);
    //     Assert.Equal(version, result.Version);
    //
    //     using (var streamReader = new StreamReader(await result.OpenReadSchemaAsync()))
    //     {
    //         var retrievedSchema = await streamReader.ReadToEndAsync();
    //         Assert.Equal(schema, retrievedSchema);
    //     }
    //
    //     result.Dispose();
    // }
    //
    // [Fact]
    // public async Task SetGatewaySchema_WithByteContent_StoresCorrectly()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var schema = "type Query { hello: String }"u8.ToArray();
    //     var settings = CreateSettingsJson();
    //     var version = new Version("2.0.0");
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     var metadata = CreateTestMetadata();
    //     await archive.SetArchiveMetadataAsync(metadata);
    //     await archive.SetGatewayConfigurationAsync(schema, settings, version);
    //
    //     // Can read immediately within the same session
    //     var result = await archive.TryGetGatewayConfigurationAsync(version);
    //
    //     Assert.NotNull(result);
    //     Assert.Equal(version, result.Version);
    //
    //     using (var streamReader = new StreamReader(await result.OpenReadSchemaAsync()))
    //     {
    //         var retrievedSchema = await streamReader.ReadToEndAsync();
    //         Assert.Equal(Encoding.UTF8.GetString(schema), retrievedSchema);
    //     }
    //
    //     result.Dispose();
    // }
    //
    // [Fact]
    // public async Task SetGatewaySchema_WithoutMetadata_ThrowsInvalidOperationException()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream);
    //     await Assert.ThrowsAsync<InvalidOperationException>(
    //         () => archive.SetGatewayConfigurationAsync("schema", CreateSettingsJson(), new Version("1.0.0")));
    // }
    //
    // [Fact]
    // public async Task SetGatewaySchema_WithUnsupportedVersion_ThrowsInvalidOperationException()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     var metadata = CreateTestMetadata();
    //     await archive.SetArchiveMetadataAsync(metadata);
    //
    //     await Assert.ThrowsAsync<InvalidOperationException>(() =>
    //         archive.SetGatewayConfigurationAsync("schema", CreateSettingsJson(), new Version("3.0.0")));
    // }
    //
    // [Fact]
    // public async Task TryGetGatewaySchema_WithCompatibleVersion_ReturnsCorrectVersion()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.0.0"), new Version("2.1.0")],
    //         SourceSchemas = ["test-service"]
    //     };
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetArchiveMetadataAsync(metadata);
    //     await archive.SetGatewayConfigurationAsync("schema v1.0", CreateSettingsJson(), new Version("1.0.0"));
    //     await archive.SetGatewayConfigurationAsync("schema v2.0", CreateSettingsJson(), new Version("2.0.0"));
    //     await archive.SetGatewayConfigurationAsync("schema v2.1", CreateSettingsJson(), new Version("2.1.0"));
    //
    //     // Request max version 2.0.0, should get 2.0.0
    //     var result = await archive.TryGetGatewayConfigurationAsync(new Version("2.0.0"));
    //
    //     Assert.NotNull(result);
    //     Assert.Equal(new Version("2.0.0"), result.Version);
    //
    //     using (var streamReader = new StreamReader(await result.OpenReadSchemaAsync()))
    //     {
    //         var retrievedSchema = await streamReader.ReadToEndAsync();
    //         Assert.Equal("schema v2.0", retrievedSchema);
    //     }
    //
    //     result.Dispose();
    // }
    //
    // [Fact]
    // public async Task TryGetGatewaySchema_WithIncompatibleVersion_ReturnsFalse()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("2.0.0")],
    //         SourceSchemas = ["test-service"]
    //     };
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetArchiveMetadataAsync(metadata);
    //
    //     var result = await archive.TryGetGatewayConfigurationAsync(new Version("1.0.0"));
    //
    //     Assert.Null(result);
    // }
    //
    // [Fact]
    // public async Task SetSourceSchema_WithValidSchema_StoresCorrectly()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var schemaContent = "type User { id: ID! name: String! }"u8.ToArray();
    //     var settings = CreateSettingsJson();
    //     const string schemaName = "user-service";
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     var metadata = CreateTestMetadata();
    //     await archive.SetArchiveMetadataAsync(metadata);
    //     await archive.SetSourceSchemaConfigurationAsync(schemaName, schemaContent, settings);
    //
    //     // Can read immediately within the same session
    //     var found = await archive.TryGetSourceSchemaConfigurationAsync(schemaName);
    //
    //     Assert.NotNull(found);
    //
    //     using var streamReader = new StreamReader(await found.OpenReadSchemaAsync());
    //     var retrievedSchema = await streamReader.ReadToEndAsync();
    //     Assert.Equal(Encoding.UTF8.GetString(schemaContent), retrievedSchema);
    // }
    //
    // [Fact]
    // public async Task SetSourceSchema_WithInvalidSchemaName_ThrowsArgumentException()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     var metadata = CreateTestMetadata();
    //     await archive.SetArchiveMetadataAsync(metadata);
    //
    //     await Assert.ThrowsAsync<ArgumentException>(
    //         () => archive.SetSourceSchemaConfigurationAsync(
    //             "invalid name!",
    //             "schema"u8.ToArray(),
    //             CreateSettingsJson()));
    // }
    //
    // [Fact]
    // public async Task SetSourceSchema_WithUndeclaredSchemaName_ThrowsInvalidOperationException()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("2.0.0")],
    //         SourceSchemas = ["declared-schema"]
    //     };
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetArchiveMetadataAsync(metadata);
    //
    //     await Assert.ThrowsAsync<InvalidOperationException>(
    //         () => archive.SetSourceSchemaConfigurationAsync(
    //             "undeclared-schema",
    //             "schema"u8.ToArray(),
    //             CreateSettingsJson()));
    // }
    //
    // [Fact]
    // public async Task TryGetSourceSchema_WithNonExistentSchema_ReturnsFalse()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     var found = await archive.TryGetSourceSchemaConfigurationAsync("non-existent");
    //     Assert.Null(found);
    // }

    // [Fact]
    // public async Task CommitAndReopen_PersistsChanges()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("2.0.0")],
    //         SourceSchemas = ["test-service"]
    //     };
    //     const string schema = "type Query { hello: String }";
    //
    //     // Act - Create and commit
    //     using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
    //     {
    //         await archive.SetArchiveMetadataAsync(metadata);
    //         await archive.SetGatewayConfigurationAsync(schema, CreateSettingsJson(), new Version("2.0.0"));
    //         await archive.CommitAsync();
    //     }
    //
    //     // Assert - Reopen and verify persistence
    //     stream.Position = 0;
    //     using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
    //     {
    //         var retrievedMetadata = await readArchive.GetArchiveMetadataAsync();
    //         Assert.NotNull(retrievedMetadata);
    //         Assert.Equal(
    //             metadata.SupportedGatewayFormats.ToArray(),
    //             retrievedMetadata.SupportedGatewayFormats.ToArray());
    //
    //         var result = await readArchive.TryGetGatewayConfigurationAsync(new Version("2.0.0"));
    //         Assert.NotNull(result);
    //
    //         using var streamReader = new StreamReader(await result.OpenReadSchemaAsync());
    //         var retrievedSchema = await streamReader.ReadToEndAsync();
    //         Assert.Equal(schema, retrievedSchema);
    //
    //         result.Dispose();
    //     }
    // }
    //
    // [Fact]
    // public async Task UpdateMode_CanModifyExistingArchive()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("2.0.0")],
    //         SourceSchemas = ["test-service"]
    //     };
    //
    //     // Act - Create initial archive
    //     using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
    //     {
    //         await archive.SetArchiveMetadataAsync(metadata);
    //         await archive.SetGatewayConfigurationAsync("original schema", CreateSettingsJson(), new Version("2.0.0"));
    //         await archive.CommitAsync();
    //     }
    //
    //     // Act - Update existing archive
    //     stream.Position = 0;
    //     using (var updateArchive = OpenApiCollectionArchive.Open(stream, OpenApiCollectionArchiveMode.Update, leaveOpen: true))
    //     {
    //         await updateArchive.SetGatewayConfigurationAsync(
    //             "modified schema",
    //             CreateSettingsJson(),
    //             new Version("2.0.0"));
    //         await updateArchive.CommitAsync();
    //     }
    //
    //     // Assert - Verify modification
    //     stream.Position = 0;
    //     using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
    //     {
    //         var result = await readArchive.TryGetGatewayConfigurationAsync(new Version("2.0.0"));
    //         Assert.NotNull(result);
    //
    //         using var streamReader = new StreamReader(await result.OpenReadSchemaAsync());
    //         var retrievedSchema = await streamReader.ReadToEndAsync();
    //         Assert.Equal("modified schema", retrievedSchema);
    //
    //         result.Dispose();
    //     }
    // }
    //
    // [Fact]
    // public async Task OverwriteFile_WithinSession_ReplacesContent()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     var metadata = CreateTestMetadata();
    //     await archive.SetArchiveMetadataAsync(metadata);
    //
    //     // Set schema twice within the same session
    //     await archive.SetGatewayConfigurationAsync("first schema", CreateSettingsJson(), new Version("2.0.0"));
    //     await archive.SetGatewayConfigurationAsync("second schema", CreateSettingsJson(), new Version("2.0.0"));
    //
    //     // Should get the last value
    //     var result = await archive.TryGetGatewayConfigurationAsync(new Version("2.0.0"));
    //
    //     Assert.NotNull(result);
    //
    //     using var streamReader = new StreamReader(await result.OpenReadSchemaAsync());
    //     var retrievedSchema = await streamReader.ReadToEndAsync();
    //     Assert.Equal("second schema", retrievedSchema);
    //
    //     result.Dispose();
    // }
    //
    // [Fact]
    // public async Task GetSourceSchemaNames_WithMetadata_ReturnsOrderedNames()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("2.0.0")],
    //         SourceSchemas = ["zebra-service", "alpha-service", "beta-service"]
    //     };
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetArchiveMetadataAsync(metadata);
    //     var names = await archive.GetSourceSchemaNamesAsync();
    //     Assert.Equal(["alpha-service", "beta-service", "zebra-service"], names);
    // }
    //
    // [Fact]
    // public async Task GetSupportedGatewayFormats_WithMetadata_ReturnsDescendingOrder()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.1.0"), new Version("2.0.0")],
    //         SourceSchemas = ["test-service"]
    //     };
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetArchiveMetadataAsync(metadata);
    //     var versions = await archive.GetSupportedGatewayFormatsAsync();
    //     Assert.Equal([new Version("2.1.0"), new Version("2.0.0"), new Version("1.0.0")], versions);
    // }
    //
    // [Theory]
    // [InlineData("valid-schema")]
    // [InlineData("Valid_Schema")]
    // [InlineData("schema123")]
    // [InlineData("_schema")]
    // public async Task SetSourceSchema_WithValidSchemaNames_Succeeds(string schemaName)
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("2.0.0")],
    //         SourceSchemas = [schemaName]
    //     };
    //
    //     // Act & Assert - Should not throw
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetArchiveMetadataAsync(metadata);
    //     await archive.SetSourceSchemaConfigurationAsync(schemaName, "schema"u8.ToArray(), CreateSettingsJson());
    // }
    //
    // [Theory]
    // [InlineData("invalid name")]
    // [InlineData("123invalid")]
    // [InlineData("")]
    // [InlineData("schema/name")]
    // public async Task SetSourceSchema_WithInvalidSchemaNames_ThrowsException(string schemaName)
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //     var metadata = new ArchiveMetadata
    //     {
    //         SupportedGatewayFormats = [new Version("2.0.0")],
    //         SourceSchemas = [schemaName]
    //     };
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
    //     await archive.SetArchiveMetadataAsync(metadata);
    //
    //     await Assert.ThrowsAsync<ArgumentException>(
    //         () => archive.SetSourceSchemaConfigurationAsync(
    //             schemaName,
    //             "schema"u8.ToArray(),
    //             CreateSettingsJson()));
    // }
    //
    // [Fact]
    // public async Task GetSupportedGatewayFormats_WithoutMetadata_ReturnsEmpty()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream);
    //     var formats = await archive.GetSupportedGatewayFormatsAsync();
    //     Assert.Empty(formats);
    // }
    //
    // [Fact]
    // public async Task GetSourceSchemaNames_WithoutMetadata_ReturnsEmpty()
    // {
    //     // Arrange
    //     await using var stream = CreateStream();
    //
    //     // Act & Assert
    //     using var archive = OpenApiCollectionArchive.Create(stream);
    //     var names = await archive.GetSourceSchemaNamesAsync();
    //     Assert.Empty(names);
    // }

    [Fact]
    public async Task AddOpenApiEndpoint_WithValidData_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id name } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync("GetUsers", operation, settings);

        // Assert - Can read immediately within the same session
        var endpoint = await archive.TryGetOpenApiEndpointAsync("GetUsers");

        Assert.NotNull(endpoint);
        Assert.Equal(operation, endpoint.Operation.ToArray());
        Assert.Equal("GET", endpoint.Settings.RootElement.GetProperty("method").GetString());
        Assert.Equal("/api/users", endpoint.Settings.RootElement.GetProperty("route").GetString());

        endpoint.Dispose();
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithMultipleEndpoints_StoresAll()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation1 = "query GetUsers { users { id name } }"u8.ToArray();
        var operation2 = "mutation CreateUser($input: CreateUserInput!) { createUser(input: $input) { id } }"u8.ToArray();
        var operation3 = "mutation DeleteUser($id: ID!) { deleteUser(id: $id) }"u8.ToArray();
        using var settings1 = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        using var settings2 = JsonDocument.Parse("""{"method": "POST", "route": "/api/users"}""");
        using var settings3 = JsonDocument.Parse("""{"method": "DELETE", "route": "/api/users/{id}"}""");
        var metadata = CreateTestMetadata();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync("GetUsers", operation1, settings1);
        await archive.AddOpenApiEndpointAsync("CreateUser", operation2, settings2);
        await archive.AddOpenApiEndpointAsync("DeleteUser", operation3, settings3);

        // Assert
        var endpoint1 = await archive.TryGetOpenApiEndpointAsync("GetUsers");
        var endpoint2 = await archive.TryGetOpenApiEndpointAsync("CreateUser");
        var endpoint3 = await archive.TryGetOpenApiEndpointAsync("DeleteUser");

        Assert.NotNull(endpoint1);
        Assert.Equal(operation1, endpoint1.Operation.ToArray());
        Assert.Equal("GET", endpoint1.Settings.RootElement.GetProperty("method").GetString());
        Assert.Equal("/api/users", endpoint1.Settings.RootElement.GetProperty("route").GetString());

        Assert.NotNull(endpoint2);
        Assert.Equal(operation2, endpoint2.Operation.ToArray());
        Assert.Equal("POST", endpoint2.Settings.RootElement.GetProperty("method").GetString());

        Assert.NotNull(endpoint3);
        Assert.Equal(operation3, endpoint3.Operation.ToArray());
        Assert.Equal("DELETE", endpoint3.Settings.RootElement.GetProperty("method").GetString());

        endpoint1.Dispose();
        endpoint2.Dispose();
        endpoint3.Dispose();
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiEndpointAsync("GetUsers", operation, settings));
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithEmptyOperation_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = ReadOnlyMemory<byte>.Empty;
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.AddOpenApiEndpointAsync("GetUsers", operation, settings));
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query { users { id } }"u8.ToArray();

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.AddOpenApiEndpointAsync("GetUsers", operation, null!));
    }

    [Fact]
    public async Task TryGetOpenApiEndpoint_WhenNotExists_ReturnsNull()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());

        var endpoint = await archive.TryGetOpenApiEndpointAsync("NonExistent");

        // Assert
        Assert.Null(endpoint);
    }

    [Fact]
    public async Task AddOpenApiEndpoint_CommitAndReopen_PersistsEndpoints()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUserById($id: ID!) { userById(id: $id) { id name email } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users/{id}"}""");
        var metadata = CreateTestMetadata();

        // Act - Create and commit
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddOpenApiEndpointAsync("GetUserById", operation, settings);
            await archive.CommitAsync();
        }

        // Assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var endpoint = await readArchive.TryGetOpenApiEndpointAsync("GetUserById");

            Assert.NotNull(endpoint);
            Assert.Equal(operation, endpoint.Operation.ToArray());
            Assert.Equal("GET", endpoint.Settings.RootElement.GetProperty("method").GetString());
            Assert.Equal("/api/users/{id}", endpoint.Settings.RootElement.GetProperty("route").GetString());

            endpoint.Dispose();
        }
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithComplexSettings_PreservesJsonStructure()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query SearchUsers($filter: UserFilter!, $first: Int) { users(filter: $filter, first: $first) { id name } }"u8.ToArray();
        const string settingsJson = """
            {
                "method": "GET",
                "route": "/api/users/search",
                "headers": {
                    "X-Api-Version": "2.0"
                },
                "queryParams": ["filter", "first"],
                "deprecated": false
            }
            """;
        using var settings = JsonDocument.Parse(settingsJson);
        var metadata = CreateTestMetadata();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync("SearchUsers", operation, settings);

        // Assert
        var endpoint = await archive.TryGetOpenApiEndpointAsync("SearchUsers");

        Assert.NotNull(endpoint);
        var retrievedSettings = endpoint.Settings.RootElement;
        Assert.Equal("GET", retrievedSettings.GetProperty("method").GetString());
        Assert.Equal("/api/users/search", retrievedSettings.GetProperty("route").GetString());
        Assert.Equal("2.0", retrievedSettings.GetProperty("headers").GetProperty("X-Api-Version").GetString());
        Assert.Equal(2, retrievedSettings.GetProperty("queryParams").GetArrayLength());
        Assert.False(retrievedSettings.GetProperty("deprecated").GetBoolean());

        endpoint.Dispose();
    }

    [Fact]
    public async Task AddOpenApiModel_WithValidData_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        var fragment = "fragment UserFields on User { id name email }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiModelAsync("UserFields", fragment);

        // Assert - Can read immediately within the same session
        var model = await archive.TryGetOpenApiModelAsync("UserFields");

        Assert.NotNull(model);
        Assert.Equal(fragment, model.Fragment.ToArray());
    }

    [Fact]
    public async Task AddOpenApiModel_WithMultipleModels_StoresAll()
    {
        // Arrange
        await using var stream = CreateStream();
        var fragment1 = "fragment UserFields on User { id name email }"u8.ToArray();
        var fragment2 = "fragment ProductFields on Product { id title price }"u8.ToArray();
        var fragment3 = "fragment OrderFields on Order { id status createdAt }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiModelAsync("UserFields", fragment1);
        await archive.AddOpenApiModelAsync("ProductFields", fragment2);
        await archive.AddOpenApiModelAsync("OrderFields", fragment3);

        // Assert
        var model1 = await archive.TryGetOpenApiModelAsync("UserFields");
        var model2 = await archive.TryGetOpenApiModelAsync("ProductFields");
        var model3 = await archive.TryGetOpenApiModelAsync("OrderFields");

        Assert.NotNull(model1);
        Assert.Equal(fragment1, model1.Fragment.ToArray());

        Assert.NotNull(model2);
        Assert.Equal(fragment2, model2.Fragment.ToArray());

        Assert.NotNull(model3);
        Assert.Equal(fragment3, model3.Fragment.ToArray());
    }

    [Fact]
    public async Task AddOpenApiModel_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();
        var fragment = "fragment UserFields on User { id }"u8.ToArray();

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiModelAsync("UserFields", fragment));
    }

    [Fact]
    public async Task AddOpenApiModel_WithEmptyFragment_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var stream = CreateStream();
        var fragment = ReadOnlyMemory<byte>.Empty;

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.AddOpenApiModelAsync("UserFields", fragment));
    }

    [Fact]
    public async Task TryGetOpenApiModel_WhenNotExists_ReturnsNull()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());

        var model = await archive.TryGetOpenApiModelAsync("NonExistent");

        // Assert
        Assert.Null(model);
    }

    [Fact]
    public async Task AddOpenApiModel_CommitAndReopen_PersistsModels()
    {
        // Arrange
        await using var stream = CreateStream();
        var fragment = "fragment UserFields on User { id name email createdAt }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // Act - Create and commit
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddOpenApiModelAsync("UserFields", fragment);
            await archive.CommitAsync();
        }

        // Assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var model = await readArchive.TryGetOpenApiModelAsync("UserFields");

            Assert.NotNull(model);
            Assert.Equal(fragment, model.Fragment.ToArray());
        }
    }

    [Fact]
    public async Task AddEndpointsAndModels_Together_StoresSeparately()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUser($id: ID!) { userById(id: $id) { ...UserFields } }"u8.ToArray();
        var fragment = "fragment UserFields on User { id name email }"u8.ToArray();
        using var endpointSettings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users/{id}"}""");
        var metadata = CreateTestMetadata();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync("GetUser", operation, endpointSettings);
        await archive.AddOpenApiModelAsync("UserFields", fragment);

        // Assert - Endpoints
        var endpoint = await archive.TryGetOpenApiEndpointAsync("GetUser");

        Assert.NotNull(endpoint);
        Assert.Equal(operation, endpoint.Operation.ToArray());

        // Assert - Models
        var model = await archive.TryGetOpenApiModelAsync("UserFields");

        Assert.NotNull(model);
        Assert.Equal(fragment, model.Fragment.ToArray());

        endpoint.Dispose();
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync("GetUsers", operation, settings);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiEndpointAsync("GetUsers", operation, settings));
    }

    [Fact]
    public async Task AddOpenApiModel_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();
        var fragment = "fragment UserFields on User { id }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiModelAsync("UserFields", fragment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiModelAsync("UserFields", fragment));
    }

    [Theory]
    [InlineData("valid-name")]
    [InlineData("Valid_Name")]
    [InlineData("name123")]
    [InlineData("_name")]
    public async Task AddOpenApiEndpoint_WithValidNames_Succeeds(string name)
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");

        // Act & Assert - Should not throw
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await archive.AddOpenApiEndpointAsync(name, operation, settings);

        var endpoint = await archive.TryGetOpenApiEndpointAsync(name);
        Assert.NotNull(endpoint);
        endpoint.Dispose();
    }

    [Theory]
    [InlineData("invalid name")]
    [InlineData("123invalid")]
    [InlineData("")]
    [InlineData("name/path")]
    public async Task AddOpenApiEndpoint_WithInvalidNames_ThrowsArgumentException(string name)
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());

        await Assert.ThrowsAsync<ArgumentException>(
            () => archive.AddOpenApiEndpointAsync(name, operation, settings));
    }

    [Fact]
    public async Task GetArchiveMetadata_ReturnsEndpointAndModelNames()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        var fragment = "fragment UserFields on User { id }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync("GetUsers", operation, settings);
        await archive.AddOpenApiModelAsync("UserFields", fragment);

        // Assert
        var retrievedMetadata = await archive.GetArchiveMetadataAsync();
        Assert.NotNull(retrievedMetadata);
        Assert.Contains("GetUsers", retrievedMetadata.Endpoints);
        Assert.Contains("UserFields", retrievedMetadata.Models);
    }

    private Stream CreateStream()
    {
        var stream = new MemoryStream();
        _streamsToDispose.Add(stream);
        return stream;
    }

    private static ArchiveMetadata CreateTestMetadata()
    {
        return new ArchiveMetadata
        {
            FormatVersion = new Version("1.0.0")
        };
    }

    public void Dispose()
    {
        foreach (var stream in _streamsToDispose)
        {
            stream.Dispose();
        }
    }
}
