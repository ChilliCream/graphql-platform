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
    private readonly Mock<ISessionService> _sessionServiceMock = new();
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly List<string> _arguments = [];
    private InteractionMode? _interactionMode;
    private Session? _session;

    public CommandBuilder()
    {
        _services
            .AddNitroCommands()
            .AddNitroServices();

        _services.AddSingleton<NitroClientContext>();
        _services.AddSingleton<INitroClientContextProvider>(
            sp => sp.GetRequiredService<NitroClientContext>());

        _services.Replace(ServiceDescriptor.Singleton(_sessionServiceMock.Object));

        AddMockedNitroClients(_services);

        _sessionServiceMock
            .SetupGet(x => x.Session)
            .Returns(() => _session);
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
        var context = CreateContext();

        return await context.ExecuteAsync(cancellationToken);
    }

    public InteractiveCommand Start()
    {
        var context = CreateContext();

        if (!context.TestConsole.Profile.Capabilities.Interactive)
        {
            throw new InvalidOperationException();
        }

        return new InteractiveCommand(context);
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
            .AddSingleton<IApisClient>(Mock.Of<IApisClient>())
            .AddSingleton<IApiKeysClient>(Mock.Of<IApiKeysClient>())
            .AddSingleton<IClientsClient>(Mock.Of<IClientsClient>())
            .AddSingleton<IEnvironmentsClient>(Mock.Of<IEnvironmentsClient>())
            .AddSingleton<IFusionConfigurationClient>(Mock.Of<IFusionConfigurationClient>())
            .AddSingleton<IMcpClient>(Mock.Of<IMcpClient>())
            .AddSingleton<IMocksClient>(Mock.Of<IMocksClient>())
            .AddSingleton<IOpenApiClient>(Mock.Of<IOpenApiClient>())
            .AddSingleton<IPersonalAccessTokensClient>(Mock.Of<IPersonalAccessTokensClient>())
            .AddSingleton<ISchemasClient>(Mock.Of<ISchemasClient>())
            .AddSingleton<IStagesClient>(Mock.Of<IStagesClient>())
            .AddSingleton<IWorkspacesClient>(Mock.Of<IWorkspacesClient>());
    }

    private CommandContext CreateContext()
    {
        var stdOutWriter = new StringWriter();
        var stdErrWriter = new StringWriter();

        var testConsole = new TestConsole();
        testConsole.Profile.Out = new AnsiConsoleOutput(stdOutWriter);

        var console = new NitroConsole(testConsole, stdOutWriter, stdErrWriter);

        _services.AddSingleton<INitroConsole>(console);

        var services = _services.BuildServiceProvider();

        var rootCommand = services.GetRequiredService<NitroRootCommand>();

        var arguments = _arguments.ToList();

        if (_interactionMode is InteractionMode.JsonOutput)
        {
            arguments.AddRange(["--output", "json"]);
        }
        else if (_interactionMode is InteractionMode.NonInteractive)
        {
            testConsole.Profile.Capabilities.Interactive = false;
        }
        else
        {
            testConsole.Profile.Capabilities.Interactive = true;
        }

        return new CommandContext(
            stdOutWriter,
            stdErrWriter,
            testConsole,
            rootCommand,
            arguments,
            services);
    }
}

internal sealed record CommandContext(
    TextWriter StdOut,
    TextWriter StdErr,
    TestConsole TestConsole,
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

internal sealed record CommandResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    string ExecutableName);

internal sealed class InteractiveCommand(CommandContext context)
{
    public void Input(string input)
    {
        context.TestConsole.Input.PushTextWithEnter(input);
    }

    public void SelectOption(int index)
    {
        for (var i = 0; i < index; i++)
        {
            context.TestConsole.Input.PushKey(ConsoleKey.DownArrow);
        }

        context.TestConsole.Input.PushKey(ConsoleKey.Enter);
    }

    public void Confirm(bool value)
    {
        context.TestConsole.Input.PushTextWithEnter(value ? "y" : "n");
    }

    public async Task<CommandResult> RunToCompletionAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.ExecuteAsync(cancellationToken);
    }
}
