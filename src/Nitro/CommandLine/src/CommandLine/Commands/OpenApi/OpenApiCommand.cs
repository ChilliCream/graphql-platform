#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class OpenApiCommand : Command
{
    public OpenApiCommand() : base("openapi")
    {
        Description = "Manage OpenAPI collections.";

        Subcommands.Add(new CreateOpenApiCollectionCommand());
        Subcommands.Add(new DeleteOpenApiCollectionCommand());
        Subcommands.Add(new ListOpenApiCollectionCommand());
        Subcommands.Add(new UploadOpenApiCollectionCommand());
        Subcommands.Add(new PublishOpenApiCollectionCommand());
        Subcommands.Add(new ValidateOpenApiCollectionCommand());
    }
}
