
using ChilliCream.Nitro.Client;
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

        services
            .AddNitroServices()
            .AddNitroClients()
            .AddSingleton<INitroConsole>(new NitroConsole(AnsiConsole.Console, Console.Out, Console.Error))
            .AddNitroCommands();

        await using var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<NitroRootCommand>();

        return await rootCommand.ExecuteAsync(args, provider, null, cts.Token);
    }
}
