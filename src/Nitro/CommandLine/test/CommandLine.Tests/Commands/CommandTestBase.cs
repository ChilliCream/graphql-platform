using System.Text;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Services;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

public abstract class CommandTestBase
    : IClassFixture<NitroCommandFixture>, IAsyncDisposable
{
    protected const string ApiId = "api-1";
    protected const string Stage = "dev";
    protected const string Tag = "v1";

    private readonly string _currentDirectory = "/some/working/directory";
    private readonly NitroCommandFixture _fixture;
    private readonly List<Stream> _files = [];
    private readonly Mock<IFileSystem> _fileSystemMock = new();
    private readonly Mock<IEnvironmentVariableProvider> _environmentVariableProviderMock = new();
    protected readonly Mock<ISchemasClient> SchemasClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IFusionConfigurationClient> FusionConfigurationClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IClientsClient> ClientsClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IApisClient> ApisClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IOpenApiClient> OpenApiClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IMcpClient> McpClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IMocksClient> MocksClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IApiKeysClient> ApiKeysClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IPersonalAccessTokensClient> PersonalAccessTokensClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IEnvironmentsClient> EnvironmentsClientMock = new(MockBehavior.Strict);
    protected readonly Mock<IStagesClient> StagesClientMock = new(MockBehavior.Strict);
    internal readonly Mock<Services.Sessions.ISessionService> SessionServiceMock = new();
    protected readonly Mock<IWorkspacesClient> WorkspacesClientMock = new(MockBehavior.Strict);
    private InteractionMode _interactionMode = InteractionMode.NonInteractive;
    private bool _authenticated = true;
    private bool _useSession;
    private bool _useSessionWithWorkspace;

    protected CommandTestBase(NitroCommandFixture fixture)
    {
        _fixture = fixture;

        _fileSystemMock.Setup(x => x.GetCurrentDirectory())
            .Returns(_currentDirectory);
    }

    protected void SetupInteractionMode(InteractionMode mode)
    {
        _interactionMode = mode;
    }

    protected void SetupNoAuthentication()
    {
        _authenticated = false;
    }

    protected void SetupSession()
    {
        _authenticated = false;
        _useSession = true;
    }

    protected void SetupSessionWithWorkspace()
    {
        _authenticated = false;
        _useSessionWithWorkspace = true;
    }

    protected async Task<CommandResult> ExecuteCommandAsync(params string[] args)
    {
        return await CreateCommandBuilder()
           .AddInteractionMode(_interactionMode)
           .AddArguments(args)
           .ExecuteAsync();
    }

    internal InteractiveCommand StartInteractiveCommand(params string[] args)
    {
        return CreateCommandBuilder()
            .AddInteractionMode(_interactionMode)
            .AddArguments(args)
            .Start();
    }

    private CommandBuilder CreateCommandBuilder()
    {
        var builder = new CommandBuilder(_fixture)
            .AddService(_fileSystemMock.Object)
            .AddService(_environmentVariableProviderMock.Object)
            .AddService(SessionServiceMock.Object)
            .AddService(WorkspacesClientMock.Object)
            .AddService(SchemasClientMock.Object)
            .AddService(FusionConfigurationClientMock.Object)
            .AddService(ClientsClientMock.Object)
            .AddService(ApisClientMock.Object)
            .AddService(OpenApiClientMock.Object)
            .AddService(McpClientMock.Object)
            .AddService(MocksClientMock.Object)
            .AddService(ApiKeysClientMock.Object)
            .AddService(PersonalAccessTokensClientMock.Object)
            .AddService(EnvironmentsClientMock.Object)
            .AddService(StagesClientMock.Object);

        if (_authenticated)
        {
            builder.AddApiKey();
        }

        if (_useSession)
        {
            SessionServiceMock
                .SetupGet(x => x.Session)
                .Returns(CreateSession(null));
        }

        if (_useSessionWithWorkspace)
        {
            SessionServiceMock
                .SetupGet(x => x.Session)
                .Returns(CreateSession(
                    new Services.Sessions.Workspace(
                        "workspace-from-session",
                        "Workspace from session")));
        }

        return builder;
    }

    private static Services.Sessions.Session CreateSession(
        Services.Sessions.Workspace? workspace)
    {
        return new Services.Sessions.Session(
            "session-1",
            "subject-1",
            "tenant-1",
            "https://id.chillicream.com",
            "api.chillicream.com",
            "user@chillicream.com",
            tokens: null,
            workspace: workspace);
    }

    protected void SetupFile(string path, string content)
    {
        SetupFile(path, new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    protected void SetupFile(string path, MemoryStream stream)
    {
        var fullPath = Path.Combine(_currentDirectory, path);

        _files.Add(stream);

        _fileSystemMock.Setup(x => x.FileExists(fullPath)).Returns(true);
        _fileSystemMock.Setup(x => x.OpenReadStream(fullPath)).Returns(stream);
        _fileSystemMock
            .Setup(x => x.ReadAllBytesAsync(fullPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream.ToArray());
        _fileSystemMock
            .Setup(x => x.ReadAllTextAsync(fullPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetString(stream.ToArray()));
    }

    protected void SetupFusionPublishingStateCache(string requestId)
    {
        var cacheFile = Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
        _fileSystemMock.Setup(x => x.FileExists(cacheFile)).Returns(true);
        _fileSystemMock
            .Setup(x => x.ReadAllTextAsync(cacheFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requestId);
    }

    protected void SetupFusionPublishingStateCacheMiss()
    {
        var cacheFile = Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
        _fileSystemMock.Setup(x => x.FileExists(cacheFile)).Returns(false);
    }

    protected void SetupOpenReadStream(string path, byte[]? content = null)
    {
        var fullPath = Path.Combine(_currentDirectory, path);
        _fileSystemMock.Setup(x => x.FileExists(fullPath)).Returns(true);
        _fileSystemMock.Setup(x => x.OpenReadStream(fullPath))
            .Returns(new MemoryStream(content ?? "archive-content"u8.ToArray()));
    }

    protected void SetupDirectory(string path, params string[] files)
    {
        var fullPath = Path.Combine(_currentDirectory, path);
        _fileSystemMock.Setup(x => x.DirectoryExists(fullPath)).Returns(true);

        if (files.Length > 0)
        {
            _fileSystemMock
                .Setup(x => x.GetFiles(fullPath, It.IsAny<string>(), It.IsAny<SearchOption>()))
                .Returns(files);
        }
    }

    /// <summary>
    /// Sets up the mock to intercept <c>CreateFile</c> for the given path.
    /// Returns an in-memory stream that receives the written content.
    /// </summary>
    protected MemoryStream SetupCreateFile(string path)
    {
        var fullPath = Path.Combine(_currentDirectory, path);
        var stream = new MemoryStream();
        _files.Add(stream);
        _fileSystemMock.Setup(x => x.CreateFile(fullPath)).Returns(() =>
        {
            stream.Position = 0;
            return stream;
        });
        return stream;
    }

    protected void SetupOpenReadStreamByPath(string path, byte[] content)
    {
        var stream = new MemoryStream(content);
        _files.Add(stream);
        _fileSystemMock.Setup(x => x.OpenReadStream(path)).Returns(stream);
    }

    protected void SetupGlobMatch(string[] results)
    {
        _fileSystemMock
            .Setup(x => x.GlobMatch(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>?>()))
            .Returns(results);
    }

    protected void SetupReadAllBytes(string path, byte[] content)
    {
        _fileSystemMock
            .Setup(x => x.ReadAllBytesAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
    }

    protected void SetupEnvironmentVariable(string variableName, string value)
    {
        _environmentVariableProviderMock
            .Setup(x => x.GetEnvironmentVariable("NITRO_" + variableName))
            .Returns(value);
    }

    protected void SetupSelectApisPrompt(
        params (string Id, string Name)[] apis)
    {
        var nodes = apis
            .Select(static a =>
                new SelectApiPromptQuery_WorkspaceById_Apis_Edges_Node_Api(
                    a.Id,
                    a.Name,
                    [],
                    null,
                    new ShowApiCommandQuery_Node_Settings_ApiSettings(
                        new ShowApiCommandQuery_Node_Settings_SchemaRegistry_SchemaRegistrySettings(false, false))))
            .ToArray<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>();

        ApisClientMock.Setup(x => x.SelectApisAsync(
                "workspace-from-session",
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>(
                nodes, null, false));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var file in _files)
        {
            await file.DisposeAsync();
        }

        // TODO: Maybe this should validate filesystem/session accesses

        SchemasClientMock.VerifyAll();
        FusionConfigurationClientMock.VerifyAll();
        ClientsClientMock.VerifyAll();
        ApisClientMock.VerifyAll();
        OpenApiClientMock.VerifyAll();
        McpClientMock.VerifyAll();
        MocksClientMock.VerifyAll();
        ApiKeysClientMock.VerifyAll();
        PersonalAccessTokensClientMock.VerifyAll();
        WorkspacesClientMock.VerifyAll();
        EnvironmentsClientMock.VerifyAll();
        StagesClientMock.VerifyAll();
    }
}
