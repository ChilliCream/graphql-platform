
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var services = new ServiceCollection();

        services.AddNitroServices();

        services.AddSingleton<NitroClientContext>();
        services.AddSingleton<INitroClientContextProvider>(sp => sp.GetRequiredService<NitroClientContext>());
        services.AddNitroClients();

        var outConsole = AnsiConsole.Console;
        var errorConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(Console.Error)
        });

        // When output is not a terminal (CI, pipes), Spectre.Console defaults
        // to 80 chars width. Use a wide width so the consumer handles wrapping.
        if (Console.IsOutputRedirected)
        {
            outConsole.Profile.Width = Constants.DefaultPrintWidth;
        }

        if (Console.IsErrorRedirected)
        {
            errorConsole.Profile.Width = Constants.DefaultPrintWidth;
        }

        services
            .AddSingleton<INitroConsole>(sp =>
                new NitroConsole(
                    outConsole,
                    errorConsole,
                    sp.GetRequiredService<IEnvironmentVariableProvider>()));

        await using var provider = services.BuildServiceProvider();

        var rootCommand = new NitroRootCommand();

        return await rootCommand.ExecuteAsync(args, provider, null, cts.Token);
    }
}
