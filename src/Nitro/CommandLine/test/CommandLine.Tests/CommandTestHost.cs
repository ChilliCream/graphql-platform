using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using CliTestConsole = System.CommandLine.IO.TestConsole;
using SpectreTestConsole = Spectre.Console.Testing.TestConsole;

namespace ChilliCream.Nitro.CommandLine.Tests;

internal sealed class CommandTestHost
{
    private readonly Dictionary<Type, object> _serviceOverrides = [];
    private readonly SpectreTestConsole _testConsole = new();
    private readonly CliTestConsole _cliConsole = new();

    public string Output => _testConsole.Output;

    public string StdOut => _cliConsole.Out?.ToString() ?? string.Empty;

    public string StdErr => _cliConsole.Error?.ToString() ?? string.Empty;

    public SpectreTestConsole Console => _testConsole;

    public CommandTestHost AddService<T>(T service)
        where T : class
    {
        _serviceOverrides[typeof(T)] = service;
        return this;
    }

    public Parser Build()
    {
        var builder = new CommandLineBuilder(new NitroRootCommand())
            .AddNitroCloudConfiguration()
            .AddService(_ => ExtendedConsole.Create(_testConsole))
            .AddService<IAnsiConsole>(sp => sp.GetRequiredService<ExtendedConsole>())
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

    public Task<int> InvokeAsync(params string[] args)
        => Build().InvokeAsync(args, _cliConsole);
}
