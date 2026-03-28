using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
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
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;
using CliTestConsole = System.CommandLine.IO.TestConsole;
using SpectreTestConsole = Spectre.Console.Testing.TestConsole;

namespace ChilliCream.Nitro.CommandLine.Tests;

internal sealed class CommandBuilder
{
    private readonly Dictionary<Type, object> _serviceOverrides = [];
    private readonly Dictionary<string, string?> _defaultOptions = new(StringComparer.Ordinal);
    private readonly SpectreTestConsole _testConsole = new();
    private readonly CliTestConsole _cliConsole = new();
    private bool _isInteractive = true;
    private InteractionMode? _interactionMode;
    private string[]? _arguments;
    private bool _consumed;

    public CommandBuilder()
    {
        _testConsole.Profile.Capabilities.Interactive = true;
    }

    public string Output => _testConsole.Output;

    public string StdOut => _cliConsole.Out?.ToString() ?? string.Empty;

    public string StdErr => _cliConsole.Error?.ToString() ?? string.Empty;

    public SpectreTestConsole Console => _testConsole;

    public CommandBuilder AddService<T>(T service)
        where T : class
    {
        ThrowIfConsumed();
        _serviceOverrides[typeof(T)] = service;
        return this;
    }

    public CommandBuilder AddInteractionMode(InteractionMode mode)
    {
        ThrowIfConsumed();
        _interactionMode = mode;
        return this;
    }

    public CommandBuilder AddApiKey()
    {
        ThrowIfConsumed();
        AddDefaultOption("--api-key", "api-key");
        return this;
    }

    public CommandBuilder AddSession()
    {
        ThrowIfConsumed();
        _serviceOverrides[typeof(ISessionService)] = new TestSessionService();
        return this;
    }

    public CommandBuilder AddSessionWithWorkspace(string workspaceId = "workspace-from-session")
    {
        ThrowIfConsumed();
        _serviceOverrides[typeof(ISessionService)] = TestSessionService.WithWorkspace(workspaceId);
        return this;
    }

    public ArgumentsBuilder Arguments => new(this);

    public readonly struct ArgumentsBuilder(CommandBuilder builder)
    {
        public CommandBuilder Adds(params string[] arguments) => builder.SetArguments(arguments);
    }

    private CommandBuilder SetArguments(params string[] arguments)
    {
        ThrowIfConsumed();

        if (_arguments is not null)
        {
            throw new InvalidOperationException("Arguments have already been configured.");
        }

        if (arguments.Length == 0)
        {
            throw new InvalidOperationException("At least one command argument is required.");
        }

        _arguments = arguments;
        return this;
    }

    // TODO: This is legacy
    public async Task<int> InvokeAsync(params string[] args)
    {
        var result = await Arguments.Adds(args).ExecuteAsync();
        return result.ExitCode;
    }

    public async Task<CommandExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureCanRun();
        _consumed = true;

        ApplyInteractionMode();

        var exitCode = await Build()
            .InvokeAsync(ApplyDefaultOptions(_arguments!), _cliConsole)
            .WaitAsync(cancellationToken);

        return CommandExecutionResult.From(this, exitCode);
    }

    public Task<InteractiveCommandExecution> StartAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureCanRun();
        _consumed = true;

        ApplyInteractionMode();

        var invocationTask = Build()
            .InvokeAsync(ApplyDefaultOptions(_arguments!), _cliConsole);

        return Task.FromResult(
            InteractiveCommandExecution.Create(
                this,
                invocationTask,
                supportsInteraction: IsInteractiveMode()));
    }

    private void EnsureCanRun()
    {
        if (_consumed)
        {
            throw new InvalidOperationException(
                "This CommandBuilder instance has already been used. Create a new instance for another run.");
        }

        if (_arguments is null)
        {
            throw new InvalidOperationException("No arguments have been configured. Call Arguments.Adds(...) first.");
        }
    }

    private void ApplyInteractionMode()
    {
        switch (_interactionMode)
        {
            case InteractionMode.Interactive:
                _isInteractive = true;
                break;

            case InteractionMode.NonInteractive:
                _isInteractive = false;
                break;

            case InteractionMode.JsonOutput:
                _isInteractive = false;
                AddDefaultOption("--output", "json");
                break;
        }
    }

    private bool IsInteractiveMode()
        => _interactionMode is null or InteractionMode.Interactive;

    private void ThrowIfConsumed()
    {
        if (_consumed)
        {
            throw new InvalidOperationException(
                "This CommandBuilder instance has already been used and cannot be reconfigured.");
        }
    }

    private Parser Build()
    {
        var services = new ServiceCollection()
            .AddSingleton<INitroConsole>(new NitroConsole(_testConsole, _cliConsole.Error))
            .AddSingleton<IFileSystem, FileSystem>()
            .AddSingleton<ISessionService, TestSessionService>()
            .AddSingleton<IApisClient>(DefaultApisClient)
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
            .AddSingleton<IWorkspacesClient>(Mock.Of<IWorkspacesClient>())
            .AddNitroCommands();

        foreach (var (type, instance) in _serviceOverrides)
        {
            services.AddSingleton(type, instance);
        }

        var serviceProvider = services.BuildServiceProvider();
        var builder = new CommandLineBuilder(serviceProvider.GetRequiredService<NitroRootCommand>());

        builder.AddMiddleware(
            context =>
            {
                var registered = new HashSet<Type>();

                AddBindingService(typeof(INitroConsole));
                AddBindingService(typeof(IFileSystem));
                AddBindingService(typeof(ISessionService));
                AddBindingService(typeof(IApisClient));
                AddBindingService(typeof(IApiKeysClient));
                AddBindingService(typeof(IClientsClient));
                AddBindingService(typeof(IEnvironmentsClient));
                AddBindingService(typeof(IFusionConfigurationClient));
                AddBindingService(typeof(IMcpClient));
                AddBindingService(typeof(IMocksClient));
                AddBindingService(typeof(IOpenApiClient));
                AddBindingService(typeof(IPersonalAccessTokensClient));
                AddBindingService(typeof(ISchemasClient));
                AddBindingService(typeof(IStagesClient));
                AddBindingService(typeof(IWorkspacesClient));

                foreach (var type in _serviceOverrides.Keys)
                {
                    AddBindingService(type);
                }

                void AddBindingService(Type type)
                {
                    if (!registered.Add(type))
                    {
                        return;
                    }

                    context.BindingContext.AddService(type, _ => serviceProvider.GetRequiredService(type));
                }
            },
            MiddlewareOrder.Configuration);

        builder
            .UseDefaults()
            .UseExceptionMiddleware();

        builder.AddMiddleware(async (context, next) =>
        {
            _testConsole.Profile.Capabilities.Interactive = _isInteractive;

            try
            {
                await next(context);
            }
            finally
            {
                // Let result formatters render JSON even after non-interactive command execution.
                _testConsole.Profile.Capabilities.Interactive = true;
            }
        });

        return builder.Build();
    }

    private void AddDefaultOption(string optionName, string? value = null)
    {
        _defaultOptions[optionName] = value;
    }

    private string[] ApplyDefaultOptions(string[] args)
    {
        if (_defaultOptions.Count == 0)
        {
            return args;
        }

        var merged = args.ToList();

        foreach (var (optionName, value) in _defaultOptions)
        {
            if (ContainsOption(args, optionName))
            {
                continue;
            }

            merged.Add(optionName);

            if (value is not null)
            {
                merged.Add(value);
            }
        }

        return [.. merged];
    }

    private static bool ContainsOption(string[] args, string optionName)
    {
        foreach (var arg in args)
        {
            if (string.Equals(arg, optionName, StringComparison.Ordinal)
                || arg.StartsWith(optionName + "=", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IApisClient CreateDefaultApisClient()
    {
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        var apiNode = new Mock<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>(MockBehavior.Strict);
        apiNode.SetupGet(x => x.Id).Returns("api-1");
        apiNode.SetupGet(x => x.Name).Returns("api-1");

        var page = new ConnectionPage<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>(
            [apiNode.Object],
            EndCursor: null,
            HasNextPage: false);

        client.Setup(x => x.SelectApisAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        return client.Object;
    }

    private static readonly IApisClient DefaultApisClient = CreateDefaultApisClient();
}

internal sealed class InteractiveCommandExecution
{
    private readonly CommandBuilder _host;
    private readonly Task<int> _invocation;
    private readonly bool _supportsInteraction;
    private Task<CommandExecutionResult>? _completion;

    private InteractiveCommandExecution(
        CommandBuilder host,
        Task<int> invocation,
        bool supportsInteraction)
    {
        _host = host;
        _invocation = invocation;
        _supportsInteraction = supportsInteraction;
    }

    public static InteractiveCommandExecution Create(
        CommandBuilder host,
        Task<int> invocation,
        bool supportsInteraction)
        => new(host, invocation, supportsInteraction);

    public Task InputAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportsInteraction(nameof(InputAsync));
        _host.Console.Input.PushTextWithEnter(input);
        return Task.CompletedTask;
    }

    public Task SelectOptionAsync(
        string option,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportsInteraction(nameof(SelectOptionAsync));

        if (string.Equals(option, "workspace", StringComparison.OrdinalIgnoreCase))
        {
            _host.Console.Input.PushKey(ConsoleKey.DownArrow);
        }

        _host.Console.Input.PushKey(ConsoleKey.Enter);
        return Task.CompletedTask;
    }

    public Task ConfirmAsync(
        bool value,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportsInteraction(nameof(ConfirmAsync));
        _host.Console.Input.PushTextWithEnter(value ? "y" : "n");
        return Task.CompletedTask;
    }

    public Task<CommandExecutionResult> RunToCompletionAsync(
        CancellationToken cancellationToken = default)
    {
        _completion ??= RunInternalAsync(cancellationToken);
        return _completion;
    }

    private async Task<CommandExecutionResult> RunInternalAsync(CancellationToken cancellationToken)
    {
        var exitCode = await _invocation
            .WaitAsync(cancellationToken);

        return CommandExecutionResult.From(_host, exitCode);
    }

    private void EnsureSupportsInteraction(string method)
    {
        if (_supportsInteraction)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cannot call {method} when the command is not running in interactive mode.");
    }
}

internal sealed class CommandExecutionResult
{
    private readonly string _stdOut;
    private readonly string _stdErr;
    private readonly string _output;

    private CommandExecutionResult(
        int exitCode,
        string stdOut,
        string stdErr,
        string output)
    {
        ExitCode = exitCode;
        _stdOut = stdOut;
        _stdErr = stdErr;
        _output = output;
    }

    public int ExitCode { get; }

    public string StdOut => _stdOut.TrimEnd();

    public string StdErr => _stdErr.TrimEnd();

    public string Output => _output.TrimEnd();

    internal static CommandExecutionResult From(CommandBuilder host, int exitCode)
    {
        var stdOut = host.StdOut;

        if (string.IsNullOrWhiteSpace(stdOut) && exitCode == 0)
        {
            stdOut = host.Output;
        }

        return new CommandExecutionResult(
            exitCode,
            stdOut,
            host.StdErr,
            host.Output);
    }
}
