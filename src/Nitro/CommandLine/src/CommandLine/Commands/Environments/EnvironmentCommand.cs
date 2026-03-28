#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class EnvironmentCommand : Command
{
    public EnvironmentCommand(
        CreateEnvironmentCommand createEnvironmentCommand,
        ListEnvironmentCommand listEnvironmentCommand,
        ShowEnvironmentCommand showEnvironmentCommand)
        : base("environment")
    {
        Description = "Manage environments";

        Subcommands.Add(createEnvironmentCommand);
        Subcommands.Add(listEnvironmentCommand);
        Subcommands.Add(showEnvironmentCommand);
    }
}
