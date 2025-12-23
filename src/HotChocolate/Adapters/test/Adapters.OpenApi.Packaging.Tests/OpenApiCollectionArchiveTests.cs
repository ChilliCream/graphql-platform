using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Adapters.OpenApi.Packaging;

public class OpenApiCollectionArchiveTests : IDisposable
{
    private readonly List<Stream> _streamsToDispose = [];

    [Fact]
    public void Create_WithNullStream_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenApiCollectionArchive.Create(null!));
    }

    [Fact]
    public void Open_WithNullStream_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenApiCollectionArchive.Open(default(Stream)!));
    }

    [Fact]
    public void Open_WithNullString_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => OpenApiCollectionArchive.Open(default(string)!));
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
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        var result = await archive.GetArchiveMetadataAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetArchiveMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.SetArchiveMetadataAsync(null!));
    }

    [Fact]
    public async Task CommitAndReopen_PersistsEndpointsAndModels()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = CreateTestMetadata();
        var operation = "query GetUsers { users { id name } }"u8.ToArray();
        var fragment = "fragment UserFields on User { id name }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // act - Create and commit
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddOpenApiEndpointAsync(key, operation, settings);
            using var modelSettings = CreateEmptyModelSettings();
            await archive.AddOpenApiModelAsync("UserFields", fragment, modelSettings);
            await archive.CommitAsync();
        }

        // assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var retrievedMetadata = await readArchive.GetArchiveMetadataAsync();
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(metadata.FormatVersion, retrievedMetadata.FormatVersion);

            var endpoint = await readArchive.TryGetOpenApiEndpointAsync(key);
            Assert.NotNull(endpoint);
            Assert.Equal(operation, endpoint.Document.ToArray());
            endpoint.Dispose();

            using var model = await readArchive.TryGetOpenApiModelAsync("UserFields");
            Assert.NotNull(model);
            Assert.Equal(fragment, model.Document.ToArray());
        }
    }

    [Fact]
    public async Task UpdateMode_CanAddNewEndpointsToExistingArchive()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = CreateTestMetadata();
        var operation1 = "query GetUsers { users { id } }"u8.ToArray();
        var operation2 = "mutation CreateUser { createUser { id } }"u8.ToArray();
        using var settings1 = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        using var settings2 = JsonDocument.Parse("""{"method": "POST", "route": "/api/users"}""");
        var key1 = new OpenApiEndpointKey("GET", "/api/users");
        var key2 = new OpenApiEndpointKey("POST", "/api/users");

        // act - Create initial archive
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddOpenApiEndpointAsync(key1, operation1, settings1);
            await archive.CommitAsync();
        }

        // act - Update existing archive
        stream.Position = 0;
        using (var updateArchive = OpenApiCollectionArchive.Open(stream, OpenApiCollectionArchiveMode.Update, leaveOpen: true))
        {
            await updateArchive.AddOpenApiEndpointAsync(key2, operation2, settings2);
            await updateArchive.CommitAsync();
        }

        // assert - Verify both endpoints exist
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var endpoint1 = await readArchive.TryGetOpenApiEndpointAsync(key1);
            Assert.NotNull(endpoint1);
            Assert.Equal(operation1, endpoint1.Document.ToArray());
            endpoint1.Dispose();

            var endpoint2 = await readArchive.TryGetOpenApiEndpointAsync(key2);
            Assert.NotNull(endpoint2);
            Assert.Equal(operation2, endpoint2.Document.ToArray());
            endpoint2.Dispose();
        }
    }

    [Fact]
    public async Task TryGetOpenApiModel_WithNonExistentModel_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        var found = await archive.TryGetOpenApiModelAsync("non-existent");
        Assert.Null(found);
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithValidData_StoresCorrectly()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id name } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, settings);

        // assert - Can read immediately within the same session
        var endpoint = await archive.TryGetOpenApiEndpointAsync(key);

        Assert.NotNull(endpoint);
        Assert.Equal(operation, endpoint.Document.ToArray());
        Assert.Equal("GET", endpoint.Settings.RootElement.GetProperty("method").GetString());
        Assert.Equal("/api/users", endpoint.Settings.RootElement.GetProperty("route").GetString());

        endpoint.Dispose();
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithMultipleEndpoints_StoresAll()
    {
        // arrange
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

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key1, operation1, settings1);
        await archive.AddOpenApiEndpointAsync(key2, operation2, settings2);
        await archive.AddOpenApiEndpointAsync(key3, operation3, settings3);

        // assert
        var endpoint1 = await archive.TryGetOpenApiEndpointAsync(key1);
        var endpoint2 = await archive.TryGetOpenApiEndpointAsync(key2);
        var endpoint3 = await archive.TryGetOpenApiEndpointAsync(key3);

        Assert.NotNull(endpoint1);
        Assert.Equal(operation1, endpoint1.Document.ToArray());
        Assert.Equal("GET", endpoint1.Settings.RootElement.GetProperty("method").GetString());
        Assert.Equal("/api/users", endpoint1.Settings.RootElement.GetProperty("route").GetString());

        Assert.NotNull(endpoint2);
        Assert.Equal(operation2, endpoint2.Document.ToArray());
        Assert.Equal("POST", endpoint2.Settings.RootElement.GetProperty("method").GetString());

        Assert.NotNull(endpoint3);
        Assert.Equal(operation3, endpoint3.Document.ToArray());
        Assert.Equal("DELETE", endpoint3.Settings.RootElement.GetProperty("method").GetString());

        endpoint1.Dispose();
        endpoint2.Dispose();
        endpoint3.Dispose();
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiEndpointAsync(key, operation, settings));
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithEmptyOperation_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = ReadOnlyMemory<byte>.Empty;
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.AddOpenApiEndpointAsync(key, operation, settings));
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithNullSettings_ThrowsArgumentNullException()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query { users { id } }"u8.ToArray();
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.AddOpenApiEndpointAsync(key, operation, null!));
    }

    [Fact]
    public async Task TryGetOpenApiEndpoint_WhenNotExists_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();
        var key = new OpenApiEndpointKey("GET", "/api/non-existent");

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());

        var endpoint = await archive.TryGetOpenApiEndpointAsync(key);

        // assert
        Assert.Null(endpoint);
    }

    [Fact]
    public async Task AddOpenApiEndpoint_CommitAndReopen_PersistsEndpoints()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUserById($id: ID!) { userById(id: $id) { id name email } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users/{id}"}""");
        var metadata = CreateTestMetadata();
        var key = new OpenApiEndpointKey("GET", "/api/users/{id}");

        // act - Create and commit
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddOpenApiEndpointAsync(key, operation, settings);
            await archive.CommitAsync();
        }

        // assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var endpoint = await readArchive.TryGetOpenApiEndpointAsync(key);

            Assert.NotNull(endpoint);
            Assert.Equal(operation, endpoint.Document.ToArray());
            Assert.Equal("GET", endpoint.Settings.RootElement.GetProperty("method").GetString());
            Assert.Equal("/api/users/{id}", endpoint.Settings.RootElement.GetProperty("route").GetString());

            endpoint.Dispose();
        }
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithComplexSettings_PreservesJsonStructure()
    {
        // arrange
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

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, settings);

        // assert
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
        // arrange
        await using var stream = CreateStream();
        var fragment = "fragment UserFields on User { id name email }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        using var settings = CreateEmptyModelSettings();
        await archive.AddOpenApiModelAsync("UserFields", fragment, settings);

        // assert - Can read immediately within the same session
        using var model = await archive.TryGetOpenApiModelAsync("UserFields");

        Assert.NotNull(model);
        Assert.Equal(fragment, model.Document.ToArray());
    }

    [Fact]
    public async Task AddOpenApiModel_WithMultipleModels_StoresAll()
    {
        // arrange
        await using var stream = CreateStream();
        var fragment1 = "fragment UserFields on User { id name email }"u8.ToArray();
        var fragment2 = "fragment ProductFields on Product { id title price }"u8.ToArray();
        var fragment3 = "fragment OrderFields on Order { id status createdAt }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        using var settings1 = CreateEmptyModelSettings();
        using var settings2 = CreateEmptyModelSettings();
        using var settings3 = CreateEmptyModelSettings();
        await archive.AddOpenApiModelAsync("UserFields", fragment1, settings1);
        await archive.AddOpenApiModelAsync("ProductFields", fragment2, settings2);
        await archive.AddOpenApiModelAsync("OrderFields", fragment3, settings3);

        // assert
        using var model1 = await archive.TryGetOpenApiModelAsync("UserFields");
        using var model2 = await archive.TryGetOpenApiModelAsync("ProductFields");
        using var model3 = await archive.TryGetOpenApiModelAsync("OrderFields");

        Assert.NotNull(model1);
        Assert.Equal(fragment1, model1.Document.ToArray());

        Assert.NotNull(model2);
        Assert.Equal(fragment2, model2.Document.ToArray());

        Assert.NotNull(model3);
        Assert.Equal(fragment3, model3.Document.ToArray());
    }

    [Fact]
    public async Task AddOpenApiModel_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var fragment = "fragment UserFields on User { id }"u8.ToArray();

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream);
        using var settings = CreateEmptyModelSettings();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiModelAsync("UserFields", fragment, settings));
    }

    [Fact]
    public async Task AddOpenApiModel_WithEmptyFragment_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        await using var stream = CreateStream();
        var fragment = ReadOnlyMemory<byte>.Empty;

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        using var settings = CreateEmptyModelSettings();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.AddOpenApiModelAsync("UserFields", fragment, settings));
    }

    [Fact]
    public async Task TryGetOpenApiModel_WhenNotExists_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());

        var model = await archive.TryGetOpenApiModelAsync("NonExistent");

        // assert
        Assert.Null(model);
    }

    [Fact]
    public async Task AddOpenApiModel_CommitAndReopen_PersistsModels()
    {
        // arrange
        await using var stream = CreateStream();
        var fragment = "fragment UserFields on User { id name email createdAt }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // act - Create and commit
        using (var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            using var settings = CreateEmptyModelSettings();
            await archive.AddOpenApiModelAsync("UserFields", fragment, settings);
            await archive.CommitAsync();
        }

        // assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            using var model = await readArchive.TryGetOpenApiModelAsync("UserFields");

            Assert.NotNull(model);
            Assert.Equal(fragment, model.Document.ToArray());
        }
    }

    [Fact]
    public async Task AddEndpointsAndModels_Together_StoresSeparately()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUser($id: ID!) { userById(id: $id) { ...UserFields } }"u8.ToArray();
        var fragment = "fragment UserFields on User { id name email }"u8.ToArray();
        using var endpointSettings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users/{id}"}""");
        var metadata = CreateTestMetadata();
        var key = new OpenApiEndpointKey("GET", "/api/users/{id}");

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, endpointSettings);
        using var modelSettings = CreateEmptyModelSettings();
        await archive.AddOpenApiModelAsync("UserFields", fragment, modelSettings);

        // assert - Endpoints
        var endpoint = await archive.TryGetOpenApiEndpointAsync(key);

        Assert.NotNull(endpoint);
        Assert.Equal(operation, endpoint.Document.ToArray());

        // assert - Models
        using var model = await archive.TryGetOpenApiModelAsync("UserFields");

        Assert.NotNull(model);
        Assert.Equal(fragment, model.Document.ToArray());

        endpoint.Dispose();
    }

    [Fact]
    public async Task AddOpenApiEndpoint_WithDuplicateKey_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, settings);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiEndpointAsync(key, operation, settings));
    }

    [Fact]
    public async Task AddOpenApiModel_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var fragment = "fragment UserFields on User { id }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // act & Assert
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        using var settings = CreateEmptyModelSettings();
        await archive.AddOpenApiModelAsync("UserFields", fragment, settings);

        using var settings2 = CreateEmptyModelSettings();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddOpenApiModelAsync("UserFields", fragment, settings2));
    }

    [Theory]
    [InlineData("GET", "/api/users")]
    [InlineData("POST", "/api/users")]
    [InlineData("PUT", "/api/users/{id}")]
    [InlineData("DELETE", "/api/users/{id}")]
    [InlineData("PATCH", "/api/users/{id}")]
    public async Task AddOpenApiEndpoint_WithValidKeys_Succeeds(string httpMethod, string route)
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var key = new OpenApiEndpointKey(httpMethod, route);

        // act & Assert - Should not throw
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
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        var fragment = "fragment UserFields on User { id }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{"method": "GET", "route": "/api/users"}""");
        var metadata = CreateTestMetadata();
        var key = new OpenApiEndpointKey("GET", "/api/users");

        // act
        using var archive = OpenApiCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddOpenApiEndpointAsync(key, operation, settings);
        using var modelSettings = CreateEmptyModelSettings();
        await archive.AddOpenApiModelAsync("UserFields", fragment, modelSettings);

        // assert
        var retrievedMetadata = await archive.GetArchiveMetadataAsync();
        Assert.NotNull(retrievedMetadata);
        Assert.Contains(key, retrievedMetadata.Endpoints);
        Assert.Contains("UserFields", retrievedMetadata.Models);
    }

    [Fact]
    public async Task Serialized_Endpoint_Should_Be_Parseable_As_OpenApiEndpoint_Definition()
    {
        // arrange
        await using var stream = CreateStream();

        var document = Utf8GraphQLParser.Parse(
            """
            "Test description"
            mutation UpdateDeeplyNestedObject($input: DeeplyNestedInput! @body)
              @http(method: PUT, route: "/object/{userId:$input.userId}", queryParameters: ["field:$input.object.field2"]) {
              updateDeeplyNestedObject(input: $input) {
                userId
                field
                object {
                  otherField
                  field2
                }
              }
            }
            """);
        var endpointDefinition = (OpenApiEndpointDefinition)OpenApiDefinitionParser.Parse(document).Definition!;
        var settingsDto = endpointDefinition.ToDto();
        var key = new OpenApiEndpointKey(endpointDefinition.HttpMethod, endpointDefinition.Route);

        using var settings = OpenApiEndpointSettingsSerializer.Format(settingsDto);

        var metadata = CreateTestMetadata();

        // act
        using (var writeArchive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await writeArchive.SetArchiveMetadataAsync(metadata);

            var ms = new MemoryStream();
            await endpointDefinition.Document.PrintToAsync(ms);
            await writeArchive.AddOpenApiEndpointAsync(key, ms.ToArray(), settings);
            await writeArchive.CommitAsync();
        }

        stream.Position = 0;

        OpenApiEndpointDefinition parsedDefinition;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var readMetadata = await readArchive.GetArchiveMetadataAsync();
            var endpointKey = readMetadata!.Endpoints[0];
            using var endpoint = await readArchive.TryGetOpenApiEndpointAsync(endpointKey);

            var readDocument = Utf8GraphQLParser.Parse(endpoint!.Document.Span);
            var readSettings = OpenApiEndpointSettingsSerializer.Parse(endpoint.Settings);

            parsedDefinition = OpenApiEndpointDefinition.From(
                readSettings,
                endpointKey.HttpMethod,
                endpointKey.Route,
                readDocument);
        }

        // assert
        Assert.Equal(endpointDefinition.HttpMethod, parsedDefinition.HttpMethod);
        Assert.Equal(endpointDefinition.Route, parsedDefinition.Route);
        Assert.Equal(endpointDefinition.Description, parsedDefinition.Description);
        Assert.Equal(endpointDefinition.BodyVariableName, parsedDefinition.BodyVariableName);
        Assert.Equal(endpointDefinition.RouteParameters.Length, parsedDefinition.RouteParameters.Length);
        Assert.Equal(endpointDefinition.QueryParameters.Length, parsedDefinition.QueryParameters.Length);

        for (var i = 0; i < endpointDefinition.RouteParameters.Length; i++)
        {
            Assert.Equal(endpointDefinition.RouteParameters[i].Key, parsedDefinition.RouteParameters[i].Key);
            Assert.Equal(endpointDefinition.RouteParameters[i].VariableName, parsedDefinition.RouteParameters[i].VariableName);
            Assert.Equal(endpointDefinition.RouteParameters[i].InputObjectPath, parsedDefinition.RouteParameters[i].InputObjectPath);
        }

        for (var i = 0; i < endpointDefinition.QueryParameters.Length; i++)
        {
            Assert.Equal(endpointDefinition.QueryParameters[i].Key, parsedDefinition.QueryParameters[i].Key);
            Assert.Equal(endpointDefinition.QueryParameters[i].VariableName, parsedDefinition.QueryParameters[i].VariableName);
            Assert.Equal(endpointDefinition.QueryParameters[i].InputObjectPath, parsedDefinition.QueryParameters[i].InputObjectPath);
        }

        Assert.Equal(endpointDefinition.Document.ToString(), parsedDefinition.Document.ToString());
    }

    [Fact]
    public async Task Serialized_Model_Should_Be_Parseable_As_OpenApiModel_Definition()
    {
        // arrange
        await using var stream = CreateStream();

        var document = Utf8GraphQLParser.Parse(
            """
            "User model with basic fields"
            fragment UserFields on User {
              id
              name
              email
            }
            """);
        var modelDefinition = (OpenApiModelDefinition)OpenApiDefinitionParser.Parse(document).Definition!;
        var settingsDto = modelDefinition.ToDto();

        using var settings = OpenApiModelSettingsSerializer.Format(settingsDto);

        var metadata = CreateTestMetadata();

        // act
        using (var writeArchive = OpenApiCollectionArchive.Create(stream, leaveOpen: true))
        {
            await writeArchive.SetArchiveMetadataAsync(metadata);

            var ms = new MemoryStream();
            await modelDefinition.Document.PrintToAsync(ms);
            await writeArchive.AddOpenApiModelAsync(modelDefinition.Name, ms.ToArray(), settings);
            await writeArchive.CommitAsync();
        }

        stream.Position = 0;

        OpenApiModelDefinition parsedDefinition;
        using (var readArchive = OpenApiCollectionArchive.Open(stream, leaveOpen: true))
        {
            var readMetadata = await readArchive.GetArchiveMetadataAsync();
            var modelName = readMetadata!.Models[0];
            using var model = await readArchive.TryGetOpenApiModelAsync(modelName);

            var readDocument = Utf8GraphQLParser.Parse(model!.Document.Span);
            var readSettings = OpenApiModelSettingsSerializer.Parse(model.Settings);

            parsedDefinition = OpenApiModelDefinition.From(
                readSettings,
                modelName,
                readDocument);
        }

        // assert
        Assert.Equal(modelDefinition.Name, parsedDefinition.Name);
        Assert.Equal(modelDefinition.Description, parsedDefinition.Description);
        Assert.Equal(modelDefinition.Document.ToString(), parsedDefinition.Document.ToString());
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

    private static JsonDocument CreateEmptyModelSettings()
    {
        return JsonDocument.Parse("""{"description": null}""");
    }

    public void Dispose()
    {
        foreach (var stream in _streamsToDispose)
        {
            stream.Dispose();
        }
    }
}
