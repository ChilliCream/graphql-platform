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

    [Fact]
    public async Task CommitAndReopen_PersistsEndpointsAndModels()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = CreateTestMetadata();
        var operation = "query GetUsers { users { id name } }"u8.ToArray();
        var fragment = "fragment UserFields on User { id name }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // Act - Create and commit
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddOpenApiEndpointAsync(key, operation, settings);
            await archive.AddOpenApiModelAsync("UserFields", fragment);
            await archive.CommitAsync();
        }

        // Assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var retrievedMetadata = await readArchive.GetArchiveMetadataAsync();
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(metadata.FormatVersion, retrievedMetadata.FormatVersion);

            var endpoint = await readArchive.TryGetOpenApiEndpointAsync(key);
            Assert.NotNull(endpoint);
            Assert.Equal(operation, endpoint.Operation.ToArray());
            endpoint.Dispose();

            var model = await readArchive.TryGetOpenApiModelAsync("UserFields");
            Assert.NotNull(model);
            Assert.Equal(fragment, model.Fragment.ToArray());
        }
    }

    [Fact]
    public async Task UpdateMode_CanAddNewEndpointsToExistingArchive()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = CreateTestMetadata();
        var operation1 = "query GetUsers { users { id } }"u8.ToArray();
        var operation2 = "mutation CreateUser { createUser { id } }"u8.ToArray();
        using var settings1 = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        using var settings2 = JsonDocument.Parse("""{"method": "POST", "route": "/api/users"}""");
        var key1 = new OpenApiEndpointKey("GET", "/api/users");
        var key2 = new OpenApiEndpointKey("POST", "/api/users");

        // Act - Create initial archive
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddOpenApiEndpointAsync(key1, operation1, settings1);
            await archive.CommitAsync();
        }

        // Act - Update existing archive
        stream.Position = 0;
        using (var updateArchive = OpenApiCollectionArchive.Open(stream, OpenApiCollectionArchiveMode.Update, leaveOpen: true))
        {
            await updateArchive.AddOpenApiEndpointAsync(key2, operation2, settings2);
            await updateArchive.CommitAsync();
        }

        // Assert - Verify both endpoints exist
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var endpoint1 = await readArchive.TryGetOpenApiEndpointAsync(key1);
            Assert.NotNull(endpoint1);
            Assert.Equal(operation1, endpoint1.Operation.ToArray());
            endpoint1.Dispose();

            var endpoint2 = await readArchive.TryGetOpenApiEndpointAsync(key2);
            Assert.NotNull(endpoint2);
            Assert.Equal(operation2, endpoint2.Operation.ToArray());
            endpoint2.Dispose();
        }
    }

    [Fact]
    public async Task TryGetOpenApiModel_WithNonExistentModel_ReturnsNull()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        var found = await archive.TryGetOpenApiModelAsync("non-existent");
        Assert.Null(found);
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithValidData_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id name } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, settings);

        // Assert - Can read immediately within the same session
        var endpoint = await archive.TryGetOpenApiEndpointAsync(key);

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
        var key1 = new OpenApiEndpointKey("GET", "/api/users");
        var key2 = new OpenApiEndpointKey("POST", "/api/users");
        var key3 = new OpenApiEndpointKey("DELETE", "/api/users/{id}");

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key1, operation1, settings1);
        await archive.AddOpenApiEndpointAsync(key2, operation2, settings2);
        await archive.AddOpenApiEndpointAsync(key3, operation3, settings3);

        // Assert
        var endpoint1 = await archive.TryGetOpenApiEndpointAsync(key1);
        var endpoint2 = await archive.TryGetOpenApiEndpointAsync(key2);
        var endpoint3 = await archive.TryGetOpenApiEndpointAsync(key3);

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
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiEndpointAsync(key, operation, settings));
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithEmptyOperation_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = ReadOnlyMemory<byte>.Empty;
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.AddOpenApiEndpointAsync(key, operation, settings));
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query { users { id } }"u8.ToArray();
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.AddOpenApiEndpointAsync(key, operation, null!));
    }

    [Fact]
    public async Task TryGetOpenApiEndpoint_WhenNotExists_ReturnsNull()
    {
        // Arrange
        await using var stream = CreateStream();
        var key = new OpenApiEndpointKey("GET", "/api/non-existent");

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());

        var endpoint = await archive.TryGetOpenApiEndpointAsync(key);

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
        var key = new OpenApiEndpointKey("GET", "/api/users/{id}");

        // Act - Create and commit
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddOpenApiEndpointAsync(key, operation, settings);
            await archive.CommitAsync();
        }

        // Assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var endpoint = await readArchive.TryGetOpenApiEndpointAsync(key);

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
        var key = new OpenApiEndpointKey("GET", "/api/users/search");

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, settings);

        // Assert
        var endpoint = await archive.TryGetOpenApiEndpointAsync(key);

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
        var key = new OpenApiEndpointKey("GET", "/api/users/{id}");

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, endpointSettings);
        await archive.AddOpenApiModelAsync("UserFields", fragment);

        // Assert - Endpoints
        var endpoint = await archive.TryGetOpenApiEndpointAsync(key);

        Assert.NotNull(endpoint);
        Assert.Equal(operation, endpoint.Operation.ToArray());

        // Assert - Models
        var model = await archive.TryGetOpenApiModelAsync("UserFields");

        Assert.NotNull(model);
        Assert.Equal(fragment, model.Fragment.ToArray());

        endpoint.Dispose();
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithDuplicateKey_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // Act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, settings);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiEndpointAsync(key, operation, settings));
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
    [InlineData("GET", "/api/users")]
    [InlineData("POST", "/api/users")]
    [InlineData("PUT", "/api/users/{id}")]
    [InlineData("DELETE", "/api/users/{id}")]
    [InlineData("PATCH", "/api/users/{id}")]
    public async Task AddOpenApiEndpoint_WithValidKeys_Succeeds(string httpMethod, string route)
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var key = new OpenApiEndpointKey(httpMethod, route);

        // Act & Assert - Should not throw
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await archive.AddOpenApiEndpointAsync(key, operation, settings);

        var endpoint = await archive.TryGetOpenApiEndpointAsync(key);
        Assert.NotNull(endpoint);
        endpoint.Dispose();
    }

    [Fact]
    public async Task GetArchiveMetadata_ReturnsEndpointKeysAndModelNames()
    {
        // Arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        var fragment = "fragment UserFields on User { id }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // Act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, settings);
        await archive.AddOpenApiModelAsync("UserFields", fragment);

        // Assert
        var retrievedMetadata = await archive.GetArchiveMetadataAsync();
        Assert.NotNull(retrievedMetadata);
        Assert.Contains(key, retrievedMetadata.Endpoints);
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
