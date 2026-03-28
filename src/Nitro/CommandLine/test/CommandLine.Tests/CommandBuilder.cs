using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Rendering;
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
    private IAnsiConsole _commandConsole;
    private bool _isInteractive = true;
    private InteractionMode? _interactionMode;
    private string[]? _arguments;
    private bool _consumed;

    public CommandBuilder()
    {
        _commandConsole = _testConsole;
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

    public CommandBuilder AddSession(string? workspaceId = null)
    {
        ThrowIfConsumed();

        var session = workspaceId is null
            ? new TestSessionService()
            : TestSessionService.WithWorkspace(workspaceId);

        _serviceOverrides[typeof(ISessionService)] = session;
        return this;
    }

    public CommandBuilder AddArguments(params string[] arguments)
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

    public async Task<int> InvokeAsync(params string[] args)
    {
        var result = await AddArguments(args).ExecuteAsync();
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

    public async Task<InteractiveCommandExecution> StartAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureCanRun();
        _consumed = true;

        ApplyInteractionMode();

        if (IsInteractiveMode())
        {
            _isInteractive = true;
            var console = new BlockingNitroConsole(_testConsole);
            _serviceOverrides[typeof(INitroConsole)] = console;

            var invocationTask = Build()
                .InvokeAsync(ApplyDefaultOptions(_arguments!), _cliConsole);

            var invocation = new InteractiveCommandInvocation(console, invocationTask);
            return InteractiveCommandExecution.CreateInteractive(this, invocation);
        }

        var run = Build().InvokeAsync(ApplyDefaultOptions(_arguments!), _cliConsole);
        return InteractiveCommandExecution.CreateNonInteractive(this, run);
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
            throw new InvalidOperationException("No arguments have been configured. Call AddArguments(...) first.");
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
        var builder = new CommandLineBuilder(new NitroRootCommand())
            .AddNitroCloudConfiguration()
            .AddService(_ =>
            {
                var console = ExtendedConsole.Create(_commandConsole);
                console.IsInteractive = _isInteractive;
                return console;
            })
            .AddService<IAnsiConsole>(sp => sp.GetRequiredService<ExtendedConsole>())
            .AddService<INitroConsole>(sp => new NitroConsole(sp.GetRequiredService<IAnsiConsole>()))
            .UseDefaults()
            .UseExceptionMiddleware();

        if (_serviceOverrides.Count > 0)
        {
            builder.AddMiddleware(
                context =>
                {
                    foreach (var (type, instance) in _serviceOverrides)
                    {
                        context.BindingContext.AddService(type, _ => instance);
                    }
                },
                MiddlewareOrder.Configuration);
        }

        builder.Command.AddNitroCloudCommands();
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
}

internal sealed class InteractiveCommandExecution
{
    private readonly CommandBuilder _host;
    private readonly Task<int> _invocation;
    private readonly InteractiveCommandInvocation? _interactiveInvocation;
    private readonly bool _supportsInteraction;
    private Task<CommandExecutionResult>? _completion;

    private InteractiveCommandExecution(
        CommandBuilder host,
        Task<int> invocation,
        InteractiveCommandInvocation? interactiveInvocation,
        bool supportsInteraction)
    {
        _host = host;
        _invocation = invocation;
        _interactiveInvocation = interactiveInvocation;
        _supportsInteraction = supportsInteraction;
    }

    public static InteractiveCommandExecution CreateInteractive(
        CommandBuilder host,
        InteractiveCommandInvocation invocation)
    {
        return new InteractiveCommandExecution(
            host,
            invocation.RunToCompletionAsync(),
            invocation,
            supportsInteraction: true);
    }

    public static InteractiveCommandExecution CreateNonInteractive(
        CommandBuilder host,
        Task<int> invocation)
    {
        return new InteractiveCommandExecution(
            host,
            invocation,
            interactiveInvocation: null,
            supportsInteraction: false);
    }

    public Task InputAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportsInteraction(nameof(InputAsync));
        return _interactiveInvocation!.InputAsync(input).WaitAsync(cancellationToken);
    }

    public Task SelectOptionAsync(
        string option,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportsInteraction(nameof(SelectOptionAsync));
        return _interactiveInvocation!.SelectOptionAsync(option).WaitAsync(cancellationToken);
    }

    public Task ConfirmAsync(
        bool value,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportsInteraction(nameof(ConfirmAsync));
        return _interactiveInvocation!.ConfirmAsync(value).WaitAsync(cancellationToken);
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

    private CommandExecutionResult(
        int exitCode,
        string stdOut,
        string stdErr)
    {
        ExitCode = exitCode;
        _stdOut = stdOut;
        _stdErr = stdErr;
    }

    public int ExitCode { get; }

    public string StdOut => _stdOut.TrimEnd();

    public string StdErr => _stdErr.TrimEnd();

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
            host.StdErr);
    }
}

internal sealed class InteractiveCommandInvocation
{
    private readonly BlockingNitroConsole _console;
    private readonly Task<int> _invocation;

    public InteractiveCommandInvocation(
        BlockingNitroConsole console,
        Task<int> invocation)
    {
        _console = console;
        _invocation = invocation;
    }

    public Task InputAsync(string input)
        => _console.EnqueueInputAsync(input).AsTask();

    public Task SelectOptionAsync(string option)
        => _console.EnqueueSelectionAsync(option).AsTask();

    public Task ConfirmAsync(bool value)
        => _console.EnqueueConfirmationAsync(value).AsTask();

    public Task<int> RunToCompletionAsync()
        => _invocation;
}

internal sealed class BlockingNitroConsole(IAnsiConsole console) : INitroConsole
{
    private readonly Channel<PromptResponse> _responses = Channel.CreateUnbounded<PromptResponse>();

    public Profile Profile => console.Profile;

    public IExclusivityMode ExclusivityMode => console.ExclusivityMode;

    public IAnsiConsoleInput Input => console.Input;

    public RenderPipeline Pipeline => console.Pipeline;

    public IAnsiConsoleCursor Cursor => console.Cursor;

    public bool IsInteractive => true;

    public void Clear(bool home)
        => console.Clear(home);

    public void Write(IRenderable renderable)
        => console.Write(renderable);

    public ValueTask EnqueueInputAsync(string input)
        => _responses.Writer.WriteAsync(PromptResponse.FromInput(input));

    public ValueTask EnqueueSelectionAsync(string option)
        => _responses.Writer.WriteAsync(PromptResponse.FromSelection(option));

    public ValueTask EnqueueConfirmationAsync(bool value)
        => _responses.Writer.WriteAsync(PromptResponse.FromConfirmation(value));

    public INitroConsoleActivity StartActivity(string title)
    {
        console.WriteLine(title);
        return new NitroConsoleActivity(console);
    }

    public void WriteLine(string message)
        => console.WriteLine(message);

    public void WriteErrorLine(string message)
        => console.WriteLine(message);

    public async Task<string> PromptAsync(
        string question,
        string? defaultValue,
        CancellationToken cancellationToken)
    {
        console.MarkupLine(question.AsQuestion());

        var response = await _responses.Reader.ReadAsync(cancellationToken);
        return string.IsNullOrEmpty(response.Text)
            ? defaultValue ?? string.Empty
            : response.Text!;
    }

    public async Task<T> PromptAsync<T>(string question, T[] items, CancellationToken cancellationToken)
        where T : notnull
    {
        console.MarkupLine(question.AsQuestion());

        var response = await _responses.Reader.ReadAsync(cancellationToken);
        var selected = response.Text;

        if (string.IsNullOrWhiteSpace(selected))
        {
            throw new ExitException("No option selected.");
        }

        if (TryResolveSelection(items, selected!, out var resolved))
        {
            return resolved;
        }

        throw new ExitException(
            $"Invalid option '{selected}'. Available options: {string.Join(", ", items)}");
    }

    public async Task<bool> ConfirmAsync(string question, CancellationToken cancellationToken)
    {
        console.MarkupLine(question.AsQuestion());

        var response = await _responses.Reader.ReadAsync(cancellationToken);

        if (response.Confirmation is { } confirmation)
        {
            return confirmation;
        }

        if (bool.TryParse(response.Text, out var parsed))
        {
            return parsed;
        }

        return string.Equals(response.Text, "y", StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(response.Text, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveSelection<T>(
        T[] items,
        string value,
        out T resolved)
    {
        if (int.TryParse(value, out var index) && index is >= 1 and <= int.MaxValue)
        {
            var zeroBased = index - 1;
            if (zeroBased < items.Length)
            {
                resolved = items[zeroBased];
                return true;
            }
        }

        foreach (var item in items)
        {
            if (item is string s)
            {
                if (string.Equals(s, value, StringComparison.OrdinalIgnoreCase))
                {
                    resolved = item;
                    return true;
                }
            }
            else if (string.Equals(item?.ToString(), value, StringComparison.Ordinal))
            {
                resolved = item;
                return true;
            }
        }

        resolved = default!;
        return false;
    }

    private readonly record struct PromptResponse(string? Text, bool? Confirmation)
    {
        public static PromptResponse FromInput(string text) => new(text, Confirmation: null);

        public static PromptResponse FromSelection(string text) => new(text, Confirmation: null);

        public static PromptResponse FromConfirmation(bool value) => new(Text: null, Confirmation: value);
    }
}
