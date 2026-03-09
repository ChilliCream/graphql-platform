#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class ApiKeyCommand : Command
{
    public ApiKeyCommand() : base("api-key")
    {
        Description = "Manage API keys";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateApiKeyCommand());
        AddCommand(new DeleteApiKeyCommand());
        AddCommand(new ListApiKeyCommand());
    }
}
