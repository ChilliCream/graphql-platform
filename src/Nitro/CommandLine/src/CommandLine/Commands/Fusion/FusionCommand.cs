#if !NET9_0_OR_GREATER
#endif
using ChilliCream.Nitro.CommandLine.Commands.Fusion.Publish;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.Settings;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.SourceSchema;
using System.Diagnostics.CodeAnalysis;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionCommand : Command
{
    public FusionCommand() : base("fusion")
    {
        Description = "Manage Fusion configurations.";

        Subcommands.Add(new FusionComposeCommand());
        Subcommands.Add(new FusionDownloadCommand());
        Subcommands.Add(new FusionMigrateCommand());
        Subcommands.Add(new FusionPublishCommand());
        Subcommands.Add(new FusionRunCommand());
        Subcommands.Add(new FusionSettingsCommand());
        Subcommands.Add(new FusionSourceSchemaCommand());
        Subcommands.Add(new FusionValidateCommand());
        Subcommands.Add(new FusionUploadCommand());
    }
}
