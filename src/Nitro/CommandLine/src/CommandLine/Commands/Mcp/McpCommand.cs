#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class McpCommand : Command
{
    public McpCommand() : base("mcp")
    {
        Description = "Manage MCP feature collections.";

        Subcommands.Add(new CreateMcpFeatureCollectionCommand());
        Subcommands.Add(new DeleteMcpFeatureCollectionCommand());
        Subcommands.Add(new ListMcpFeatureCollectionCommand());
        Subcommands.Add(new UploadMcpFeatureCollectionCommand());
        Subcommands.Add(new PublishMcpFeatureCollectionCommand());
        Subcommands.Add(new ValidateMcpFeatureCollectionCommand());
    }
}
