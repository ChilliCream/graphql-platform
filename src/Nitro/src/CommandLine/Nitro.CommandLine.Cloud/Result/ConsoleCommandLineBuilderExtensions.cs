using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Option.Binders;

namespace ChilliCream.Nitro.CLI.Results;

internal static class ConsoleCommandLineBuilderExtensions
{
    public static CommandLineBuilder AddConsole(this CommandLineBuilder builder)
    {
        return builder
            .AddMiddleware(x =>
                {
                    if (x.Console is not IAnsiConsole ansiConsole)
                    {
                        ansiConsole = AnsiConsole.Console;
                    }

                    var customConsole = ExtendedConsole.Create(ansiConsole);
                    x.BindingContext.AddService<ExtendedConsole>(_ => customConsole);
                },
                MiddlewareOrder.Configuration)
            .AddService<IAnsiConsole>(sp => sp.GetRequiredService<ExtendedConsole>());
    }
}
