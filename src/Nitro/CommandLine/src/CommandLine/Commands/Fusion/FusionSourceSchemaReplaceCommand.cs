using System.Text.Json;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionSourceSchemaReplaceCommand : Command
{
    public FusionSourceSchemaReplaceCommand() : base("replace")
    {
        Description =
            "Replace a source schema in a Fusion archive with an updated source schema and recompose.";

        Arguments.Add(Opt<FusionReplaceSourceSchemaNameArgument>.Instance);

        Options.Add(Opt<FusionArchiveFileOption>.Instance);
        Options.Add(Opt<SourceSchemaFileOption>.Instance);
        Options.Add(Opt<FusionEnvironmentOption>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion source-schema replace reviews \
              --archive ./gateway.far \
              --source-schema-file ./reviews/schema.graphqls
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

        var oldSourceSchemaName = parseResult.GetRequiredValue(Opt<FusionReplaceSourceSchemaNameArgument>.Instance);
        var archiveFile = parseResult.GetRequiredValue(Opt<FusionArchiveFileOption>.Instance);
        var sourceSchemaFile = parseResult.GetRequiredValue(Opt<SourceSchemaFileOption>.Instance);
        var environment = parseResult.GetValue(Opt<FusionEnvironmentOption>.Instance);
        var workingDirectory = parseResult.GetValue(Opt<WorkingDirectoryOption>.Instance)
            ?? fileSystem.GetCurrentDirectory();

        archiveFile = FusionSourceSchemaHelpers.ResolveExistingArchiveFile(fileSystem, archiveFile);

        var (schemaName, sourceText, settings) = await FusionCompositionHelpers.ReadSourceSchemaAsync(
            fileSystem,
            workingDirectory,
            sourceSchemaFile,
            cancellationToken);

        var newSourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>
        {
            [schemaName] = (sourceText, settings)
        };

        environment ??= environmentVariables.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        return await FusionSourceSchemaHelpers.ApplyAndRecomposeAsync(
            archiveFile,
            MutateAsync,
            newSourceSchemas,
            environment,
            console,
            cancellationToken);

        async Task MutateAsync(FusionArchive archive)
        {
            var names = (await archive.GetSourceSchemaNamesAsync(cancellationToken)).ToList();

            if (!names.Contains(oldSourceSchemaName))
            {
                throw new ExitException(
                    Messages.SourceSchemaDoesNotExistInArchive(oldSourceSchemaName, archiveFile));
            }

            await archive.RemoveSourceSchemaConfigurationAsync(oldSourceSchemaName, cancellationToken);
        }
    }
}
