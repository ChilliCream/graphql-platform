using System.CommandLine.Builder;
using System.CommandLine.Parsing;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using ChilliCream.Nitro.CommandLine.Cloud;
using ChilliCream.Nitro.CommandLine.Fusion;

namespace ChilliCream.Nitro.CommandLine
{
#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new CommandLineBuilder(new NitroRootCommand())
                .AddNitroCloudConfiguration()
                .UseDefaults()
                .UseExceptionMiddleware()
                .UseExtendedConsole();

            var (_, fusionCommand) = builder.Command.AddNitroCloudCommands();
            fusionCommand.AddFusionComposeCommand();
            await builder.Build().InvokeAsync(args);
        }
    }
}
