using System.CommandLine;
using System.Text;
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
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

public abstract class CommandTestBase
    : IClassFixture<NitroCommandFixture>, IAsyncLifetime
{
    protected const string ApiId = "api-1";
    protected const string Stage = "dev";
    protected const string Tag = "v1";

    private readonly string _currentDirectory = "/some/working/directory";
    private readonly NitroCommandFixture _fixture;
    private readonly List<Stream> _files = [];
    private readonly Mock<IFileSystem> _fileSystemMock = new();
    private IFileSystem? _fileSystemOverride;
    private IStandardInputReader? _standardInputOverride;
    private IGlobalConfigDirectoryProvider? _globalConfigDirectoryProviderOverride;
    private Services.Hook.IClaudeSettingsPathResolver? _claudeSettingsPathResolverOverride;
    private Services.Hook.ICodexPathResolver? _codexPathResolverOverride;
    private Services.Hook.ICodexQueueClient? _codexQueueClientOverride;
    private Services.Notify.IClaudePeerClient? _claudePeerClientOverride;
    private IActingActorResolver? _actingActorResolverOverride;
    protected readonly FakeTimeProvider FakeTime =
        new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly Mock<IEnvironmentVariableProvider> _environmentVariableProviderMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
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
    internal readonly Mock<Services.Sessions.ISessionService> _sessionServiceMock = new();
    internal readonly Mock<IBrowserLauncher> _browserLauncherMock = new();
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

    /// <summary>
    /// Replaces the mocked file system with the given implementation.
    /// </summary>
    private protected void SetupFileSystem(IFileSystem fileSystem)
    {
        _fileSystemOverride = fileSystem;
    }

    /// <summary>
    /// Configures global configuration lookup to use <paramref name="directory"/>.
    /// </summary>
    private protected void SetupGlobalConfigDirectory(string directory)
    {
        _globalConfigDirectoryProviderOverride = new FixedGlobalConfigDirectoryProvider(directory);
    }

    /// <summary>
    /// Sets standard input to a reader over <paramref name="payload"/>.
    /// Command invocations share the reader until this method is called again.
    /// </summary>
    protected void SetupStandardInput(string payload)
    {
        _standardInputOverride = new FixedStandardInputReader(payload);
    }

    /// <summary>
    /// Configures Claude Code settings lookup to use the supplied user and project paths.
    /// </summary>
    private protected void SetupClaudeSettingsPathResolver(string userScopePath, string projectScopePath)
    {
        _claudeSettingsPathResolverOverride = new FixedClaudeSettingsPathResolver(userScopePath, projectScopePath);
    }

    /// <summary>
    /// Configures Codex CLI hooks and configuration lookup to use the supplied paths.
    /// </summary>
    private protected void SetupCodexPathResolver(string hooksJsonPath, string configTomlPath)
    {
        _codexPathResolverOverride = new FixedCodexPathResolver(hooksJsonPath, configTomlPath);
    }

    /// <summary>
    /// Configures commands to use the supplied <c>codex queue</c> client.
    /// </summary>
    private protected void SetupCodexQueueClient(Services.Hook.ICodexQueueClient client)
    {
        _codexQueueClientOverride = client;
    }

    /// <summary>
    /// Configures commands to use the supplied Claude peer client.
    /// </summary>
    private protected void SetupClaudePeerClient(Services.Notify.IClaudePeerClient client)
    {
        _claudePeerClientOverride = client;
    }

    /// <summary>
    /// Configures actor resolution to return <paramref name="actor"/> when the option value is null.
    /// Explicit values are normalized without checking whether the actor was allocated.
    /// </summary>
    protected void SetupActingActor(string actor)
    {
        _actingActorResolverOverride = new FixedActingActorResolver(actor);
    }

    /// <summary>
    /// Restores the production <see cref="IActingActorResolver"/> for subsequent command invocations.
    /// </summary>
    protected void SetupRealActingActor()
    {
        _actingActorResolverOverride = null;
    }

    protected void SetupNoAuthentication()
    {
        _authenticated = false;
    }

    protected void SetupHttpClient(HttpClient client)
    {
        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(client);
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

    /// <summary>
    /// The actor <see cref="ExecuteCommandAsync"/> supplies to any command
    /// that declares a required <c>--actor</c> option and was not given one
    /// explicitly. Null leaves the arguments untouched.
    /// </summary>
    private protected string? DefaultActor { get; set; }

    protected async Task<CommandResult> ExecuteCommandAsync(params string[] args)
    {
        var arguments = args.ToList();

        if (_authenticated)
        {
            arguments.AddRange(["--api-key", "default-api-key"]);
        }

        AddDefaultActorIfRequired(arguments);

        var stdOut = new SynchronizedOutputCapture();
        var stdErr = new SynchronizedOutputCapture();

        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(stdOut.Writer);
        outConsole.Profile.Width = Constants.DefaultPrintWidth;

        var errConsole = new TestConsole();
        errConsole.Profile.Out = new AnsiConsoleOutput(stdErr.Writer);
        errConsole.Profile.Width = Constants.DefaultPrintWidth;

        if (_interactionMode is InteractionMode.JsonOutput)
        {
            // Simulate a real terminal (TTY): '--output json' must force
            // non-interactive behavior even when the console is interactive.
            outConsole.Profile.Capabilities.Interactive = true;
            arguments.AddRange(["--output", "json"]);
        }
        else if (_interactionMode is InteractionMode.NonInteractive)
        {
            outConsole.Profile.Capabilities.Interactive = false;
        }
        else
        {
            outConsole.Profile.Capabilities.Interactive = true;
        }

        var console = new NitroConsole(
            outConsole,
            errConsole,
            new SnapshotActivitySinkFactory());
        var services = BuildServices(console);
        var rootCommand = _fixture.RootCommand;

        var invocationConfig = new InvocationConfiguration
        {
            Output = stdOut.Writer,
            Error = stdErr.Writer
        };

        var exitCode = await rootCommand.ExecuteAsync(arguments, services, invocationConfig, default);

        return new CommandResult(
            exitCode,
            stdOut.GetOutput().TrimEnd(),
            stdErr.GetOutput().TrimEnd(),
            rootCommand.Name);
    }

    /// <summary>
    /// Appends <c>--actor</c> with <see cref="DefaultActor"/> when the resolved command requires it
    /// and the arguments omit it. Leaves arguments unchanged when <see cref="DefaultActor"/> is null.
    /// </summary>
    private void AddDefaultActorIfRequired(List<string> arguments)
    {
        if (DefaultActor is null || arguments.Contains("--actor"))
        {
            return;
        }

        // Resolve the subcommand from leading command-name arguments.
        Command command = _fixture.RootCommand;

        foreach (var token in arguments)
        {
            if (token.StartsWith('-'))
            {
                break;
            }

            if (command.Subcommands.FirstOrDefault(c => c.Name == token) is not { } subcommand)
            {
                break;
            }

            command = subcommand;
        }

        if (command.Options.Any(o => o.Name == "--actor" && o.Required))
        {
            arguments.AddRange(["--actor", DefaultActor]);
        }
    }

    internal InteractiveCommand StartInteractiveCommand(params string[] args)
    {
        var arguments = args.ToList();

        if (_authenticated)
        {
            arguments.AddRange(["--api-key", "default-api-key"]);
        }

        AddDefaultActorIfRequired(arguments);

        if (_interactionMode is InteractionMode.JsonOutput)
        {
            arguments.AddRange(["--output", "json"]);
        }

        var stdOut = new SynchronizedOutputCapture();
        var stdErr = new SynchronizedOutputCapture();

        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(stdOut.Writer);
        outConsole.Profile.Width = Constants.DefaultPrintWidth;
        outConsole.Profile.Capabilities.Interactive = true;

        var errConsole = new TestConsole();
        errConsole.Profile.Out = new AnsiConsoleOutput(stdErr.Writer);
        errConsole.Profile.Width = Constants.DefaultPrintWidth;

        var console = new NitroConsole(
            outConsole,
            errConsole,
            new SnapshotActivitySinkFactory());
        var services = BuildServices(console);
        var rootCommand = _fixture.RootCommand;

        return new InteractiveCommand(
            async cancellationToken =>
            {
                var invocationConfig = new InvocationConfiguration
                {
                    Output = stdOut.Writer,
                    Error = stdErr.Writer
                };

                var exitCode = await rootCommand.ExecuteAsync(
                    arguments, services, invocationConfig, cancellationToken);

                return new CommandResult(
                    exitCode,
                    stdOut.GetOutput().TrimEnd(),
                    stdErr.GetOutput().TrimEnd(),
                    rootCommand.Name);
            },
            outConsole,
            stdOut.GetOutput,
            stdErr.GetOutput);
    }

    private ServiceProvider BuildServices(INitroConsole console)
    {
        var services = new ServiceCollection();

        services.AddNitroServices();

        services.AddSingleton<NitroClientContext>();
        services.AddSingleton<INitroClientContextProvider>(
            sp => sp.GetRequiredService<NitroClientContext>());

        if (_useSession)
        {
            _sessionServiceMock
                .SetupGet(x => x.Session)
                .Returns(CreateSession(null));
        }

        if (_useSessionWithWorkspace)
        {
            _sessionServiceMock
                .SetupGet(x => x.Session)
                .Returns(CreateSession(
                    new Services.Sessions.Workspace(
                        "workspace-from-session",
                        "Workspace from session")));
        }

        services.Replace(ServiceDescriptor.Singleton(_fileSystemOverride ?? _fileSystemMock.Object));

        if (_globalConfigDirectoryProviderOverride is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(_globalConfigDirectoryProviderOverride));
        }

        if (_claudeSettingsPathResolverOverride is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(_claudeSettingsPathResolverOverride));
        }

        if (_codexPathResolverOverride is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(_codexPathResolverOverride));
        }

        if (_codexQueueClientOverride is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(_codexQueueClientOverride));
        }

        if (_claudePeerClientOverride is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(_claudePeerClientOverride));
        }

        if (_actingActorResolverOverride is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(_actingActorResolverOverride));
        }

        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(FakeTime));
        services.Replace(ServiceDescriptor.Singleton(_environmentVariableProviderMock.Object));

        if (_standardInputOverride is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(_standardInputOverride));
        }

        services.Replace(ServiceDescriptor.Singleton(_httpClientFactoryMock.Object));
        services.Replace(ServiceDescriptor.Singleton(_sessionServiceMock.Object));
        services.Replace(ServiceDescriptor.Singleton(_browserLauncherMock.Object));
        services.Replace(ServiceDescriptor.Singleton(WorkspacesClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(SchemasClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(FusionConfigurationClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(ClientsClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(ApisClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(OpenApiClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(McpClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(MocksClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(ApiKeysClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(PersonalAccessTokensClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(EnvironmentsClientMock.Object));
        services.Replace(ServiceDescriptor.Singleton(StagesClientMock.Object));
        services.AddSingleton(console);

        return services.BuildServiceProvider();
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
    /// Configures mocked <c>CreateFile</c> calls for <paramref name="path"/>, resolved against
    /// the test working directory, to return the same in-memory stream. Returns that stream.
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

    protected void SetupGlobMatch(string[] results)
    {
        var absoluteResults = results
            .Select(r => Path.Combine(_currentDirectory, r))
            .ToArray();

        _fileSystemMock
            .Setup(x => x.GlobMatch(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<string?>()))
            .Returns(absoluteResults);
    }

    protected void SetupReadAllBytes(string path, byte[] content)
    {
        _fileSystemMock
            .Setup(x => x.ReadAllBytesAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
    }

    protected void SetupEnvironmentVariable(string variableName, string? value)
    {
        _environmentVariableProviderMock
            .Setup(x => x.GetEnvironmentVariable("NITRO_" + variableName))
            .Returns(value);
    }

    /// <summary>
    /// Configures environment lookup for the exact <paramref name="variableName"/>
    /// to return <paramref name="value"/>.
    /// </summary>
    protected void SetupRawEnvironmentVariable(string variableName, string? value)
    {
        _environmentVariableProviderMock
            .Setup(x => x.GetEnvironmentVariable(variableName))
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

    protected void VerifyWorkspaceSelected(string workspaceId, string workspaceName)
    {
        _sessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.Is<Services.Sessions.Workspace>(w => w.Id == workspaceId && w.Name == workspaceName),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    protected void VerifyNoWorkspaceSelected()
    {
        _sessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.IsAny<Services.Sessions.Workspace>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public virtual async ValueTask DisposeAsync()
    {
        foreach (var file in _files)
        {
            await file.DisposeAsync();
        }

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

public sealed record CommandResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    string ExecutableName);
internal sealed class FixedGlobalConfigDirectoryProvider(string directory) : IGlobalConfigDirectoryProvider
{
    public string GetDirectory() => directory;
}

internal sealed class FixedClaudeSettingsPathResolver(string userScopePath, string projectScopePath)
    : Services.Hook.IClaudeSettingsPathResolver
{
    public string Resolve(string scope)
        => scope == Services.Hook.HookInstallScopes.Project ? projectScopePath : userScopePath;
}

internal sealed class FixedCodexPathResolver(string hooksJsonPath, string configTomlPath)
    : Services.Hook.ICodexPathResolver
{
    public string ResolveHooksJson() => hooksJsonPath;

    public string ResolveConfigToml() => configTomlPath;
}

internal sealed class InteractiveCommand(
    Func<CancellationToken, Task<CommandResult>> executeAsync,
    TestConsole testConsole,
    Func<string> standardOutput,
    Func<string> standardError)
{
    public string StdOut => standardOutput();

    public string StdErr => standardError();

    public void Input(string input)
    {
        testConsole.Input.PushTextWithEnter(input);
    }

    public void SelectOption(int index)
    {
        for (var i = 0; i < index; i++)
        {
            testConsole.Input.PushKey(ConsoleKey.DownArrow);
        }

        testConsole.Input.PushKey(ConsoleKey.Enter);
    }

    public void Confirm(bool value)
    {
        testConsole.Input.PushTextWithEnter(value ? "y" : "n");
    }

    public async Task<CommandResult> RunToCompletionAsync(
        CancellationToken cancellationToken = default)
    {
        return await executeAsync(cancellationToken);
    }
}
