using System.CommandLine;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.HttpClient;

public class NitroClientRegistrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ConfigureApiKeyAuth_When_ApiKeyOptionProvided()
    {
        // Act
        await using var provider = await BuildAndExecuteAsync(["--api-key", "my-key"]);
        using var client = CreateApiClient(provider);

        // Assert
        var apiKeyHeader = Assert.Single(client.DefaultRequestHeaders.GetValues("CCC-api-key"));
        Assert.Equal("my-key", apiKeyHeader);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ConfigureBearerAuth_When_SessionPresent()
    {
        // Arrange
        var session = CreateSessionWithTokens(accessToken: "my-token");

        // Act
        await using var provider = await BuildAndExecuteAsync([], session);
        using var client = CreateApiClient(provider);

        // Assert
        var authHeader = Assert.Single(client.DefaultRequestHeaders.GetValues("Authorization"));
        Assert.Equal("Bearer my-token", authHeader);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PreferApiKey_Over_SessionToken()
    {
        // Arrange
        var session = CreateSessionWithTokens(accessToken: "session-token");

        // Act
        await using var provider = await BuildAndExecuteAsync(["--api-key", "cli-key"], session);
        using var client = CreateApiClient(provider);

        // Assert
        var apiKeyHeader = Assert.Single(client.DefaultRequestHeaders.GetValues("CCC-api-key"));
        Assert.Equal("cli-key", apiKeyHeader);
        Assert.False(client.DefaultRequestHeaders.Contains("Authorization"));
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseExplicitCloudUrl()
    {
        // Act
        await using var provider = await BuildAndExecuteAsync(
            ["--api-key", "x", "--cloud-url", "custom.host.com"]);
        using var client = CreateApiClient(provider);

        // Assert
        Assert.Equal(
            new Uri("https://custom.host.com/graphql"),
            client.BaseAddress);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseSessionUrl_When_NoExplicitUrl()
    {
        // Arrange
        var session = CreateSessionWithTokens(apiUrl: "session-api.chillicream.com");

        // Act
        await using var provider = await BuildAndExecuteAsync([], session);
        using var client = CreateApiClient(provider);

        // Assert
        Assert.Equal(
            new Uri("https://session-api.chillicream.com/graphql"),
            client.BaseAddress);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseDefaultUrl_When_NoSessionAndNoExplicitUrl()
    {
        // Act
        await using var provider = await BuildAndExecuteAsync(["--api-key", "x"]);
        using var client = CreateApiClient(provider);

        // Assert
        Assert.Equal(
            new Uri("https://api.chillicream.com/graphql"),
            client.BaseAddress);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotSetAuth_When_NoAuthAvailable()
    {
        // Act
        await using var provider = await BuildAndExecuteAsync([]);
        using var client = CreateApiClient(provider);

        // Assert
        Assert.False(client.DefaultRequestHeaders.Contains("CCC-api-key"));
        Assert.False(client.DefaultRequestHeaders.Contains("Authorization"));
    }

    private static async Task<ServiceProvider> BuildAndExecuteAsync(
        string[] args,
        Session? session = null)
    {
        var services = new ServiceCollection();
        services.AddNitroServices();

        var sessionMock = new Mock<ISessionService>();
        sessionMock
            .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionMock.SetupGet(x => x.Session).Returns(session);
        services.Replace(ServiceDescriptor.Singleton(sessionMock.Object));

        services
            .AddSingleton(Mock.Of<IApisClient>())
            .AddSingleton(Mock.Of<IApiKeysClient>())
            .AddSingleton(Mock.Of<IClientsClient>())
            .AddSingleton(Mock.Of<IEnvironmentsClient>())
            .AddSingleton(Mock.Of<IFusionConfigurationClient>())
            .AddSingleton(Mock.Of<IMcpClient>())
            .AddSingleton(Mock.Of<IMocksClient>())
            .AddSingleton(Mock.Of<IOpenApiClient>())
            .AddSingleton(Mock.Of<IPersonalAccessTokensClient>())
            .AddSingleton(Mock.Of<ISchemasClient>())
            .AddSingleton(Mock.Of<IStagesClient>())
            .AddSingleton(Mock.Of<IWorkspacesClient>());

        services.AddSingleton<NitroClientContext>();
        services.AddSingleton<INitroClientContextProvider>(
            sp => sp.GetRequiredService<NitroClientContext>());
        services.AddNitroClients();

        var testConsole = new TestConsole();
        var errorConsole = new TestConsole();
        services.AddSingleton<INitroConsole>(
            new NitroConsole(testConsole, errorConsole, new EnvironmentVariableProvider()));

        var provider = services.BuildServiceProvider();
        var rootCommand = new NitroRootCommand();

        var probeCommand = new Command("__probe");
        probeCommand.AddGlobalNitroOptions();
        probeCommand.SetAction((_, _) => Task.FromResult(0));
        rootCommand.Add(probeCommand);

        var invocationConfig = new InvocationConfiguration
        {
            Output = TextWriter.Null,
            Error = TextWriter.Null
        };

        await rootCommand.ExecuteAsync(
            ["__probe", .. args], provider, invocationConfig, CancellationToken.None);

        return provider;
    }

    private static System.Net.Http.HttpClient CreateApiClient(ServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        return factory.CreateClient(ApiClient.ClientName);
    }

    private static Session CreateSessionWithTokens(
        string apiUrl = "api.chillicream.com",
        string accessToken = "test-access-token")
    {
        return new Session(
            "session-1",
            "subject-1",
            "tenant-1",
            "https://id.chillicream.com",
            apiUrl,
            "user@chillicream.com",
            new Tokens(accessToken, "id-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1)),
            workspace: null);
    }
}
