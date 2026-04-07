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
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests;

internal sealed class CommandBuilder
{
    private readonly NitroCommandFixture? _fixture;
    private readonly Mock<ISessionService> _sessionServiceMock = new();
    private readonly Mock<IEnvironmentVariableProvider> _environmentVariableProviderMock = new();
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly List<string> _arguments = [];
    private InteractionMode? _interactionMode;
    private Session? _session;

    public Mock<ISessionService> SessionServiceMock => _sessionServiceMock;

    public Mock<IEnvironmentVariableProvider> EnvironmentVariableProviderMock => _environmentVariableProviderMock;

    public CommandBuilder()
    {
        _services.AddNitroServices();

        _services.AddSingleton<NitroClientContext>();
        _services.AddSingleton<INitroClientContextProvider>(
            sp => sp.GetRequiredService<NitroClientContext>());

        _services.Replace(ServiceDescriptor.Singleton(_sessionServiceMock.Object));
        _services.Replace(ServiceDescriptor.Singleton(_environmentVariableProviderMock.Object));

        AddMockedNitroClients(_services);

        _sessionServiceMock
            .SetupGet(x => x.Session)
            .Returns(() => _session);
    }

    public CommandBuilder(NitroCommandFixture fixture) : this()
    {
        _fixture = fixture;
    }

    public CommandBuilder AddInteractionMode(InteractionMode mode)
    {
        _interactionMode = mode;
        return this;
    }

    public CommandBuilder AddApiKey()
    {
        _arguments.Add("--api-key");
        _arguments.Add("default-api-key");
        return this;
    }

    public CommandBuilder AddSession()
    {
        _session = CreateSession(null);
        return this;
    }

    public CommandBuilder AddSessionWithWorkspace(string workspaceId = "workspace-from-session")
    {
        _session = CreateSession(new Workspace(workspaceId, "Workspace from session"));
        return this;
    }

    public CommandBuilder AddArguments(params string[] arguments)
    {
        _arguments.InsertRange(0, arguments);

        return this;
    }

    public CommandBuilder AddService<T>(T service)
        where T : class
    {
        _services.Replace(ServiceDescriptor.Singleton(service));

        return this;
    }

    public async Task<CommandResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var context = CreateNonInteractiveContext();

        return await context.ExecuteAsync(cancellationToken);
    }

    public InteractiveCommand Start()
    {
        var context = CreateInteractiveContext();

        return new InteractiveCommand(context, (TestConsole)context.Console);
    }

    private static Session CreateSession(Workspace? workspace)
    {
        return new Session(
            "session-1",
            "subject-1",
            "tenant-1",
            "https://id.chillicream.com",
            "api.chillicream.com",
            "user@chillicream.com",
            tokens: null,
            workspace: workspace);
    }

    private static void AddMockedNitroClients(IServiceCollection services)
    {
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
    }

    private CommandContext CreateNonInteractiveContext()
    {
        var stdOutWriter = new StringWriter();
        var stdErrWriter = new StringWriter();

        var outConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(stdOutWriter)
        });
        outConsole.Profile.Width = 10_000;

        var errConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(stdErrWriter)
        });
        errConsole.Profile.Width = 10_000;

        var arguments = _arguments.ToList();

        if (_interactionMode is InteractionMode.JsonOutput)
        {
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

        return CreateContext(stdOutWriter, stdErrWriter, outConsole, errConsole, arguments);
    }

    private CommandContext CreateInteractiveContext()
    {
        var stdOutWriter = new StringWriter();
        var stdErrWriter = new StringWriter();

        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(stdOutWriter);
        outConsole.Profile.Width = 10_000;
        outConsole.Profile.Capabilities.Interactive = true;

        var errConsole = new TestConsole();
        errConsole.Profile.Out = new AnsiConsoleOutput(stdErrWriter);
        errConsole.Profile.Width = 10_000;

        return CreateContext(stdOutWriter, stdErrWriter, outConsole, errConsole, [.._arguments]);
    }

    private CommandContext CreateContext(
        StringWriter stdOutWriter,
        StringWriter stdErrWriter,
        IAnsiConsole outConsole,
        IAnsiConsole errConsole,
        List<string> arguments)
    {
        var console = new NitroConsole(outConsole, errConsole, _environmentVariableProviderMock.Object);

        _services.AddSingleton<INitroConsole>(console);

        var services = _services.BuildServiceProvider();

        var rootCommand = _fixture?.RootCommand ?? new NitroRootCommand();

        return new CommandContext(
            stdOutWriter,
            stdErrWriter,
            outConsole,
            rootCommand,
            arguments,
            services);
    }
}

internal sealed record CommandContext(
    TextWriter StdOut,
    TextWriter StdErr,
    IAnsiConsole Console,
    NitroRootCommand RootCommand,
    IReadOnlyList<string> Arguments,
    IServiceProvider Services)
{
    public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var invocationConfig = new InvocationConfiguration
        {
            Output = StdOut,
            Error = StdErr
        };

        var exitCode = await RootCommand.ExecuteAsync(Arguments, Services, invocationConfig, cancellationToken);

        return new CommandResult(
            exitCode,
            StdOut.ToString()?.TrimEnd() ?? string.Empty,
            StdErr.ToString()?.TrimEnd() ?? string.Empty,
            RootCommand.Name);
    }
}

public sealed record CommandResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    string ExecutableName);

internal sealed class InteractiveCommand(CommandContext context, TestConsole testConsole)
{
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
        return await context.ExecuteAsync(cancellationToken);
    }
}
