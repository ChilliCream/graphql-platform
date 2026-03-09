#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Launch;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class LaunchCommand : Command
{
    public LaunchCommand() : base("launch")
    {
        Description = "Launch Nitro in your default browser";

        this.AddNitroCloudDefaultOptions();

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static Task<int> ExecuteAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        SystemBrowser.Open(Constants.NitroWebUrl);
        console.OkLine($"[link={Constants.NitroWebUrl}]Nitro[/] is launched!");

        return Task.FromResult(ExitCodes.Success);
    }
}
