using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Tools;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Management;

public sealed class ManagementToolsTests
{
    [Fact]
    public async Task CreateApiTool_Returns_Error_When_Not_Authenticated()
    {
        var sessionService = new FakeSessionService(null);
        var managementService = CreateDummyManagementService();

        var result = await CreateApiTool.ExecuteAsync(
            sessionService, managementService, "test-api");

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("Not authenticated", error.Error);
    }

    [Fact]
    public async Task CreateApiTool_Returns_Error_When_No_Workspace()
    {
        var session = CreateSessionWithoutWorkspace();
        var sessionService = new FakeSessionService(session);
        var managementService = CreateDummyManagementService();

        var result = await CreateApiTool.ExecuteAsync(
            sessionService, managementService, "test-api");

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("No workspace", error.Error);
    }

    [Fact]
    public async Task ListApisTool_Returns_Error_When_Not_Authenticated()
    {
        var sessionService = new FakeSessionService(null);
        var managementService = CreateDummyManagementService();

        var result = await ListApisTool.ExecuteAsync(
            sessionService, managementService);

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("Not authenticated", error.Error);
    }

    [Fact]
    public async Task ListApisTool_Returns_Error_When_No_Workspace()
    {
        var session = CreateSessionWithoutWorkspace();
        var sessionService = new FakeSessionService(session);
        var managementService = CreateDummyManagementService();

        var result = await ListApisTool.ExecuteAsync(
            sessionService, managementService);

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("No workspace", error.Error);
    }

    [Fact]
    public async Task CreateApiKeyTool_Returns_Error_When_Not_Authenticated()
    {
        var sessionService = new FakeSessionService(null);
        var managementService = CreateDummyManagementService();

        var result = await CreateApiKeyTool.ExecuteAsync(
            sessionService, managementService, "test-key");

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("Not authenticated", error.Error);
    }

    [Fact]
    public async Task ListApiKeysTool_Returns_Error_When_Not_Authenticated()
    {
        var sessionService = new FakeSessionService(null);
        var managementService = CreateDummyManagementService();

        var result = await ListApiKeysTool.ExecuteAsync(
            sessionService, managementService);

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("Not authenticated", error.Error);
    }

    [Fact]
    public async Task CreateClientTool_Returns_Error_When_Not_Authenticated()
    {
        var mcpContext = new ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.NitroMcpContext("api-123", "production");
        var sessionService = new FakeSessionService(null);
        var managementService = CreateDummyManagementService();

        var result = await CreateClientTool.ExecuteAsync(
            mcpContext, sessionService, managementService, "test-client");

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("Not authenticated", error.Error);
    }

    [Fact]
    public async Task ListClientsTool_Returns_Error_When_Not_Authenticated()
    {
        var mcpContext = new ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.NitroMcpContext("api-123", "production");
        var sessionService = new FakeSessionService(null);
        var managementService = CreateDummyManagementService();

        var result = await ListClientsTool.ExecuteAsync(
            mcpContext, sessionService, managementService);

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("Not authenticated", error.Error);
    }

    [Fact]
    public async Task UpdateApiSettingsTool_Returns_Error_When_Not_Authenticated()
    {
        var mcpContext = new ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.NitroMcpContext("api-123", "production");
        var sessionService = new FakeSessionService(null);
        var managementService = CreateDummyManagementService();

        var result = await UpdateApiSettingsTool.ExecuteAsync(
            mcpContext, sessionService, managementService);

        var error = JsonSerializer.Deserialize<ManagementError>(
            result, ManagementJsonContext.Default.ManagementError);
        Assert.NotNull(error);
        Assert.Contains("Not authenticated", error.Error);
    }

    [Fact]
    public void ManagementError_Serialization_Roundtrip()
    {
        var error = new ManagementError { Error = "test error" };
        var json = JsonSerializer.Serialize(error, ManagementJsonContext.Default.ManagementError);
        var deserialized = JsonSerializer.Deserialize<ManagementError>(
            json, ManagementJsonContext.Default.ManagementError);

        Assert.NotNull(deserialized);
        Assert.Equal("test error", deserialized.Error);
    }

    [Fact]
    public void CreateApiResult_Serialization_Roundtrip()
    {
        var result = new CreateApiResult
        {
            Success = true,
            Api = new ApiEntry
            {
                Id = "api-1",
                Name = "Test API",
                Path = "/",
                Kind = "SERVICE"
            }
        };

        var json = JsonSerializer.Serialize(result, ManagementJsonContext.Default.CreateApiResult);
        var deserialized = JsonSerializer.Deserialize<CreateApiResult>(
            json, ManagementJsonContext.Default.CreateApiResult);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Api);
        Assert.Equal("api-1", deserialized.Api.Id);
        Assert.Equal("Test API", deserialized.Api.Name);
    }

    [Fact]
    public void CreateApiKeyResult_Serialization_Includes_Secret()
    {
        var result = new CreateApiKeyResult
        {
            Success = true,
            ApiKey = new ApiKeyEntry { Id = "key-1", Name = "deploy-key" },
            Secret = "sk-secret-value"
        };

        var json = JsonSerializer.Serialize(result, ManagementJsonContext.Default.CreateApiKeyResult);
        var deserialized = JsonSerializer.Deserialize<CreateApiKeyResult>(
            json, ManagementJsonContext.Default.CreateApiKeyResult);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.Equal("sk-secret-value", deserialized.Secret);
    }

    [Fact]
    public void ListApisResult_Serialization_Roundtrip()
    {
        var result = new ListApisResult
        {
            Apis = new[]
            {
                new ApiEntry { Id = "api-1", Name = "API One", Path = "/", Kind = "SERVICE" },
                new ApiEntry { Id = "api-2", Name = "API Two", Path = "/team", Kind = "GATEWAY" }
            },
            PageInfo = new PageInfoEntry { HasNextPage = true, EndCursor = "cursor-123" }
        };

        var json = JsonSerializer.Serialize(result, ManagementJsonContext.Default.ListApisResult);
        var deserialized = JsonSerializer.Deserialize<ListApisResult>(
            json, ManagementJsonContext.Default.ListApisResult);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Apis.Count);
        Assert.True(deserialized.PageInfo.HasNextPage);
        Assert.Equal("cursor-123", deserialized.PageInfo.EndCursor);
    }

    [Fact]
    public void ListClientsResult_Serialization_Roundtrip()
    {
        var result = new ListClientsResult
        {
            Clients = new[]
            {
                new ClientEntry { Id = "client-1", Name = "Web App" }
            },
            PageInfo = new PageInfoEntry { HasNextPage = false }
        };

        var json = JsonSerializer.Serialize(result, ManagementJsonContext.Default.ListClientsResult);
        var deserialized = JsonSerializer.Deserialize<ListClientsResult>(
            json, ManagementJsonContext.Default.ListClientsResult);

        Assert.NotNull(deserialized);
        Assert.Single(deserialized.Clients);
        Assert.False(deserialized.PageInfo.HasNextPage);
    }

    private static ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services.ManagementService CreateDummyManagementService()
    {
        // Create a ManagementService with a NitroApiService that has a fake HTTP client.
        // The service won't actually be called in auth-error tests.
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") };
        var factory = new FakeHttpClientFactory(httpClient);
        var apiService = new ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services.NitroApiService(factory);
        return new ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services.ManagementService(apiService);
    }

    private static Session CreateSessionWithoutWorkspace()
    {
        return new Session(
            "session-1", "subject-1", "tenant-1",
            "https://identity.example.com",
            "api.example.com",
            "test@example.com",
            new Tokens("access-token", "id-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1)),
            workspace: null);
    }

    private sealed class FakeSessionService : ISessionService
    {
        private readonly Session? _session;

        public FakeSessionService(Session? session)
        {
            _session = session;
        }

        public Session? Session => _session;

        public Task<Session?> LoadSessionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_session);
        }

        public Task<Session> SelectWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Session> LoginAsync(string? authority, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }
}
