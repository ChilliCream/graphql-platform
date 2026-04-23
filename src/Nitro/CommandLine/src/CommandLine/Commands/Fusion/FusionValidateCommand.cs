#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO.Compression;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Commands.Schemas;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionValidateCommand : Command
{
    public FusionValidateCommand() : base("validate")
    {
        Description = "Validate a Fusion configuration against a stage.";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OptionalFusionArchiveFileOption>.Instance);
        Options.Add(Opt<OptionalLegacyFusionArchiveFileOption>.Instance);
        Options.Add(Opt<OptionalSourceSchemaFileListOption>.Instance);
        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion validate \
              --api-id "<api-id>" \
              --stage "dev" \
              --source-schema-file ./products/schema.graphqls \
              --source-schema-file ./reviews/schema.graphqls
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fusionConfigurationClient = services.GetRequiredService<IFusionConfigurationClient>();
        var schemasClient = services.GetRequiredService<ISchemasClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var stageName = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var archiveFile = parseResult.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance);
        var legacyArchiveFile = parseResult.GetValue(Opt<OptionalLegacyFusionArchiveFileOption>.Instance);
        var sourceSchemaFiles =
            parseResult.GetValue(Opt<OptionalSourceSchemaFileListOption>.Instance) ?? [];

        var exclusiveOptionsCount = new[]
        {
            sourceSchemaFiles is { Count: > 0 },
            archiveFile is not null
        }.Count(x => x);

        if (exclusiveOptionsCount > 1)
        {
            throw new ExitException(
                $"The options '{OptionalSourceSchemaFileListOption.OptionName}' and '{FusionArchiveFileOption.OptionName}' are mutually exclusive.");
        }
        else if (exclusiveOptionsCount < 1)
        {
            throw new ExitException(
                $"Missing one of the required options '{OptionalSourceSchemaFileListOption.OptionName}' or '{FusionArchiveFileOption.OptionName}'.");
        }

        if (archiveFile is not null)
        {
            if (legacyArchiveFile is not null)
            {
                throw new ExitException(
                    $"The options '{FusionArchiveFileOption.OptionName}' and '{OptionalLegacyFusionArchiveFileOption.OptionName}' are mutually exclusive.");
            }

            return await ValidateWithArchive();
        }
        else
        {
            if (legacyArchiveFile is not null)
            {
                if (!Path.IsPathRooted(legacyArchiveFile))
                {
                    legacyArchiveFile = Path.Combine(fileSystem.GetCurrentDirectory(), legacyArchiveFile);
                }

                if (!fileSystem.FileExists(legacyArchiveFile))
                {
                    throw new ExitException(Messages.LegacyArchiveFileDoesNotExist(legacyArchiveFile));
                }
            }

            return await ValidateWithSourceSchemaFiles();
        }

        async Task<int> ValidateWithArchive()
        {
            if (!Path.IsPathRooted(archiveFile))
            {
                archiveFile = Path.Combine(fileSystem.GetCurrentDirectory(), archiveFile);
            }

            if (!fileSystem.FileExists(archiveFile))
            {
                throw new ExitException(Messages.ArchiveFileDoesNotExist(archiveFile));
            }

            await using var archiveStream = fileSystem.OpenReadStream(archiveFile);
            await using var activity = StartActivity();

            return await ValidateAsync(activity, archiveStream);
        }

        async Task<int> ValidateWithSourceSchemaFiles()
        {
            var newSourceSchemas = await FusionCompositionHelpers.ReadSourceSchemasAsync(
                fileSystem,
                workingDirectory: null,
                sourceSchemaFiles,
                ct);

            await using var activity = StartActivity();
            await using var archiveStream = await FusionPublishHelpers.PrepareComposedArchiveAsync(
                activity,
                apiId,
                stageName,
                legacyArchiveFile,
                newSourceSchemas,
                fusionConfigurationClient,
                fileSystem,
                console,
                ct);

            return await ValidateAsync(activity, archiveStream);
        }

        async Task<int> ValidateAsync(INitroConsoleActivity activity, Stream archiveStream)
        {
            IDisposable disposableArchive;
            Stream schemaStream;

            if (IsFarFormat(archiveStream))
            {
                var archive = FusionArchive.Open(archiveStream, leaveOpen: true);

                schemaStream = await LoadSchemaFile(archive, ct);

                disposableArchive = archive;
            }
            else
            {
                var package = FusionGraphPackage.Open(archiveStream, FileAccess.Read);

                schemaStream = await LoadSchemaFile(package, ct);

                disposableArchive = package;
            }

            try
            {
                var validationResult = await SchemaHelpers.ValidateSchemaAsync(
                    activity,
                    console,
                    schemasClient,
                    apiId,
                    stageName,
                    schemaStream,
                    source: null,
                    ct);

                if (validationResult is SchemaValidationResult.Failed failed)
                {
                    activity.Fail(failed.Details, "Fusion configuration failed validation.");

                    throw new ExitException("Fusion configuration failed validation.");
                }

                activity.Success("Fusion configuration passed validation.");

                return ExitCodes.Success;
            }
            finally
            {
                disposableArchive.Dispose();
            }
        }

        INitroConsoleActivity StartActivity()
        {
            return console.StartActivity(
                $"Validating Fusion configuration of API '{apiId.EscapeMarkup()}' against stage '{stageName.EscapeMarkup()}'",
                "Failed to validate the Fusion configuration.");
        }
    }

    private static async Task<Stream> LoadSchemaFile(FusionArchive archive, CancellationToken ct)
    {
        var latestVersion = await archive.GetLatestSupportedGatewayFormatAsync(ct);
        var configuration = await archive.TryGetGatewayConfigurationAsync(latestVersion, ct);

        if (configuration is null)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve gateway configuration from the Fusion archive (format version: {latestVersion}). "
                + "The archive may be corrupted, unsupported, or missing required configuration.");
        }

        return await configuration.OpenReadSchemaAsync(ct);
    }

    private static async Task<Stream> LoadSchemaFile(FusionGraphPackage package, CancellationToken ct)
    {
        var schemaNode = await package.GetSchemaAsync(ct);

        var schemaFileStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(schemaFileStream, leaveOpen: true);
        await streamWriter.WriteAsync(schemaNode.ToString());
        await streamWriter.FlushAsync(ct);
        schemaFileStream.Position = 0;

        return schemaFileStream;
    }

    public static bool IsFarFormat(Stream stream)
    {
        try
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            return zip.GetEntry("archive-metadata.json") is not null;
        }
        catch
        {
            return false;
        }
    }
}
