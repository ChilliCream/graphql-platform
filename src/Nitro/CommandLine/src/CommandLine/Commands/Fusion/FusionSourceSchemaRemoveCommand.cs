#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionSourceSchemaRemoveCommand : Command
{
    public FusionSourceSchemaRemoveCommand() : base("remove")
    {
        Description =
            "Remove a source schema from a Fusion archive and recompose the remaining source schemas.";

        Arguments.Add(Opt<FusionSourceSchemaNameArgument>.Instance);

        Options.Add(Opt<FusionArchiveFileOption>.Instance);
        Options.Add(Opt<FusionEnvironmentOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion source-schema remove reviews \
              --archive ./gateway.far
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();

        var sourceSchemaName = parseResult.GetRequiredValue(Opt<FusionSourceSchemaNameArgument>.Instance);
        var archiveFile = parseResult.GetRequiredValue(Opt<FusionArchiveFileOption>.Instance);
        var environment = parseResult.GetValue(Opt<FusionEnvironmentOption>.Instance);

        archiveFile = FusionSourceSchemaHelpers.ResolveExistingArchiveFile(
            fileSystem,
            archiveFile,
            fileSystem.GetCurrentDirectory());

        environment ??= environmentVariables.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        return await FusionSourceSchemaHelpers.ApplyAndRecomposeAsync(
            fileSystem,
            archiveFile,
            MutateAsync,
            [],
            environment,
            console,
            cancellationToken);

        async Task MutateAsync(FusionArchive archive)
        {
            var names = (await archive.GetSourceSchemaNamesAsync(cancellationToken)).ToList();

            if (!names.Contains(sourceSchemaName))
            {
                throw new ExitException(
                    Messages.SourceSchemaDoesNotExistInArchive(sourceSchemaName, archiveFile));
            }

            if (names.Count == 1)
            {
                throw new ExitException(Messages.CannotRemoveLastSourceSchema(sourceSchemaName));
            }

            await archive.RemoveSourceSchemaConfigurationAsync(sourceSchemaName, cancellationToken);
        }
    }
}
