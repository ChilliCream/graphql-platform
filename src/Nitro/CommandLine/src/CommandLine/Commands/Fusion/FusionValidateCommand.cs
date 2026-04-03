#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
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
        Options.Add(Opt<OptionalSourceSchemaFileListOption>.Instance);
        this.AddGlobalNitroOptions();

        Validators.Add(result =>
        {
            var exclusiveOptionsCount = new[]
            {
                result.GetValue(Opt<OptionalSourceSchemaFileListOption>.Instance) is { Count: > 0 },
                result.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance) is not null
            }.Count(x => x);

            if (exclusiveOptionsCount > 1)
            {
                result.AddError(
                    $"The options '{OptionalSourceSchemaFileListOption.OptionName}' and '{FusionArchiveFileOption.OptionName}' are mutually exclusive.");
            }
            else if (exclusiveOptionsCount < 1)
            {
                result.AddError(
                    $"Missing one of the required options '{OptionalSourceSchemaFileListOption.OptionName}' or '{FusionArchiveFileOption.OptionName}'.");
            }
        });

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
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var stageName = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var archiveFile = parseResult.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance);
        var sourceSchemaFiles =
            parseResult.GetValue(Opt<OptionalSourceSchemaFileListOption>.Instance) ?? [];
        var isValid = false;
        CompositionLog? failedLog = null;

        await using (var activity = console.StartActivity(
                         $"Validating Fusion configuration against stage '{stageName.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'",
                         "Failed to validate the Fusion configuration."))
        {
            if (archiveFile is not null)
            {
                await ValidateWithArchive(activity);
            }
            else
            {
                await ValidateWithSourceSchemaFiles(activity);
            }
        }

        if (failedLog is { IsEmpty: false })
        {
            FusionComposeCommand.WriteCompositionLog(failedLog, console.Out, false);
        }

        return isValid ? ExitCodes.Success : ExitCodes.Error;

        async Task ValidateWithSourceSchemaFiles(INitroConsoleActivity activity)
        {
            Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas;
            await using (var child = activity.StartChildActivity(
                             "Reading source schema file(s)",
                             "Failed to read source schema file(s)."))
            {
                newSourceSchemas = await FusionComposeCommand.ReadSourceSchemasAsync(
                    fileSystem,
                    sourceSchemaFiles,
                    ct);

                child.Success($"Read {newSourceSchemas.Count} source schema file(s).");
            }

            var archiveStream = new MemoryStream();
            Stream? existingArchiveStream;
            await using (var child = activity.StartChildActivity(
                             "Downloading existing Fusion configuration",
                             "Failed to download existing Fusion configuration."))
            {
                existingArchiveStream = await fusionConfigurationClient.DownloadLatestFusionArchiveAsync(
                    apiId,
                    stageName,
                    WellKnownVersions.LatestGatewayFormatVersion.ToString(),
                    ct);

                if (existingArchiveStream is null)
                {
                    child.Warning("No existing Fusion configuration.");
                }
                else
                {
                    child.Success("Downloaded existing Fusion configuration.");
                }
            }

            await using (var child = activity.StartChildActivity(
                             "Composing new Fusion configuration",
                             "Failed to compose new Fusion configuration."))
            {
                var composeResult = await FusionPublishHelpers.ComposeAsync(
                    archiveStream,
                    existingArchiveStream,
                    stageName,
                    newSourceSchemas,
                    compositionSettings: null,
                    ct);

                if (!composeResult.Success)
                {
                    failedLog = composeResult.Log;
                    isValid = false;
                    return;
                }

                child.Success("Composed new Fusion configuration.");
            }

            using var archive = FusionArchive.Open(archiveStream);
            await using var schemaStream = await LoadSchemaFile(archive, ct);

            await using (var child = activity.StartChildActivity(
                             "Validating against stage",
                             "Failed to validate against stage."))
            {
                await ValidateSchemaAsync(child, schemaStream);
            }

            if (isValid)
            {
                activity.Success("Fusion configuration is valid.");
            }
        }

        async Task ValidateWithArchive(INitroConsoleActivity activity)
        {
            Stream? schemaStream = null;
            IDisposable? disposableArchive = null;

            try
            {
                await using (var child = activity.StartChildActivity(
                                 "Validating against stage",
                                 "Failed to validate against stage."))
                {
                    await using var stream = fileSystem.OpenReadStream(archiveFile);

                    if (IsFarFormat(stream))
                    {
                        var archive = FusionArchive.Open(stream, leaveOpen: true);

                        schemaStream = await LoadSchemaFile(archive, ct);

                        disposableArchive = archive;
                    }
                    else
                    {
                        var package = FusionGraphPackage.Open(stream, FileAccess.Read);

                        schemaStream = await LoadSchemaFile(package, ct);

                        disposableArchive = package;
                    }

                    await ValidateSchemaAsync(child, schemaStream);
                }

                if (isValid)
                {
                    activity.Success("Fusion configuration is valid.");
                }
            }
            finally
            {
                if (schemaStream is not null)
                {
                    await schemaStream.DisposeAsync();
                }

                disposableArchive?.Dispose();
            }
        }

        async Task ValidateSchemaAsync(INitroConsoleActivity activity, Stream schemaStream)
        {
            var requestId = await StartSchemaValidationAsync(
                activity,
                console,
                fusionConfigurationClient,
                apiId,
                stageName,
                schemaStream,
                ct);

            activity.Update($"Validation request created (ID: {requestId.EscapeMarkup()})");

            await foreach (var @event in fusionConfigurationClient
                               .SubscribeToSchemaVersionValidationUpdatedAsync(requestId, ct))
            {
                switch (@event)
                {
                    case ISchemaVersionValidationFailed { Errors: var schemaErrors}:
                        activity.FailAll();

                        foreach (var error in schemaErrors)
                        {
                            switch (error)
                            {
                                case ISchemaVersionChangeViolationError e:
                                    console.PrintSchemaVersionChangeViolations(e);
                                    break;
                                case IInvalidGraphQLSchemaError e:
                                    console.PrintGraphQLSchemaErrors(e);
                                    break;
                                case IPersistedQueryValidationError e:
                                    console.PrintPersistedQueryValidationErrors(e);
                                    break;
                                case IOpenApiCollectionValidationError e:
                                    console.PrintOpenApiCollectionValidationErrors(e);
                                    break;
                                case IMcpFeatureCollectionValidationError e:
                                    console.PrintMcpFeatureCollectionValidationErrors(e);
                                    break;
                                case IOperationsAreNotAllowedError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                                case ISchemaVersionSyntaxError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                                case IProcessingTimeoutError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                                case IUnexpectedProcessingError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                            }
                        }

                        console.Error.WriteErrorLine("Fusion configuration validation failed.");

                        isValid = false;
                        return;

                    case ISchemaVersionValidationSuccess:
                        isValid = true;
                        activity.Success("Validation passed.");

                        if (!console.IsHumanReadable)
                        {
                            resultHolder.SetResult(new ObjectResult(new FusionValidateResult(isValid)));
                        }

                        return;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("Validating...");
                        break;

                    default:
                        activity.Update("Unknown server response. Consider updating the CLI.",
                            ActivityUpdateKind.Warning);
                        break;
                }
            }
        }
    }

    private static async Task<string> StartSchemaValidationAsync(
        INitroConsoleActivity activity,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        string apiId,
        string stageName,
        Stream schema,
        CancellationToken ct)
    {
        var result = await fusionConfigurationClient.ValidateSchemaVersionAsync(
            apiId,
            stageName,
            schema,
            ct);

        if (result.Errors?.Count > 0)
        {
            activity.FailAll();

            foreach (var error in result.Errors)
            {
                var errorMessage = error switch
                {
                    IValidateSchemaVersion_ValidateSchema_Errors_UnauthorizedOperation err => err.Message,
                    IValidateSchemaVersion_ValidateSchema_Errors_ApiNotFoundError err => err.Message,
                    IValidateSchemaVersion_ValidateSchema_Errors_StageNotFoundError err => err.Message,
                    IValidateSchemaVersion_ValidateSchema_Errors_SchemaNotFoundError err => err.Message,
                    IError err => "Unexpected mutation error: " + err.Message,
                    _ => "Unexpected mutation error."
                };

                console.Error.WriteErrorLine(errorMessage);
            }

            throw new ExitException();
        }

        if (string.IsNullOrWhiteSpace(result.Id))
        {
            throw new ExitException("Could not create validation request!");
        }

        return result.Id;
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

    public record FusionValidateResult(bool IsValid);
}
