#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionCommand : Command
{
    public FusionCommand(
        FusionComposeCommand fusionComposeCommand,
        FusionDownloadCommand fusionDownloadCommand,
        FusionMigrateCommand fusionMigrateCommand,
        FusionPublishCommand fusionPublishCommand,
        FusionRunCommand fusionRunCommand,
        FusionSettingsCommand fusionSettingsCommand,
        FusionValidateCommand fusionValidateCommand,
        FusionUploadCommand fusionUploadCommand)
        : base("fusion")
    {
        Description = "Manage Fusion configurations.";

        Subcommands.Add(fusionComposeCommand);
        Subcommands.Add(fusionDownloadCommand);
        Subcommands.Add(fusionMigrateCommand);
        Subcommands.Add(fusionPublishCommand);
        Subcommands.Add(fusionRunCommand);
        Subcommands.Add(fusionSettingsCommand);
        Subcommands.Add(fusionValidateCommand);
        Subcommands.Add(fusionUploadCommand);
    }
}
