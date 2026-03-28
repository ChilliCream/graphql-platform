#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO.Compression;
using System.Net;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
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
        Description = "Validates the composed GraphQL schema of a Fusion configuration against a stage.";

        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<OptionalFusionArchiveFileOption>.Instance);
        AddOption(Opt<OptionalSourceSchemaFileListOption>.Instance);
        this.AddNitroCloudDefaultOptions();

        AddValidator(result =>
        {
            var exclusiveOptionsCount = new[]
            {
                result.FindResultFor(Opt<OptionalSourceSchemaFileListOption>.Instance) is not null,
                result.FindResultFor(Opt<OptionalFusionArchiveFileOption>.Instance) is not null
            }.Count(x => x);

            if (exclusiveOptionsCount > 1)
            {
                result.ErrorMessage =
                    $"You can only specify one of: '{OptionalSourceSchemaFileListOption.OptionName}' or '{FusionArchiveFileOption.OptionName}'.";
            }
            else if (exclusiveOptionsCount < 1)
            {
                result.ErrorMessage =
                    $"You need to specify one of: '{OptionalSourceSchemaFileListOption.OptionName}' or '{FusionArchiveFileOption.OptionName}'.";
            }
        });

        this.SetHandler(async context =>
        {
            var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var archiveFile = context.ParseResult.GetValueForOption(Opt<OptionalFusionArchiveFileOption>.Instance);
            var sourceSchemaFiles =
                context.ParseResult.GetValueForOption(Opt<OptionalSourceSchemaFileListOption>.Instance) ?? [];

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var fusionConfigurationClient =
                context.BindingContext.GetRequiredService<IFusionConfigurationClient>();
            var fileSystem = context.BindingContext.GetRequiredService<IFileSystem>();

            context.ExitCode = await ExecuteAsync(
                stageName,
                apiId,
                archiveFile,
                sourceSchemaFiles,
                console,
                fusionConfigurationClient,
                fileSystem,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        string stageName,
        string apiId,
        string? archiveFile,
        List<string> sourceSchemaFiles,
        IAnsiConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        var isValid = false;

        await using (var activity = console.StartActivity("Validating..."))
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

        return isValid ? ExitCodes.Success : ExitCodes.Error;

        async Task ValidateWithSourceSchemaFiles(ICommandLineActivity activity)
        {
            var newSourceSchemas = await FusionComposeCommand.ReadSourceSchemasAsync(
                fileSystem,
                sourceSchemaFiles,
                ct);

            var archiveStream = new MemoryStream();
            // TODO: Needs to handle old and new archive
            Stream? existingArchiveStream;
            try
            {
                existingArchiveStream = await fusionConfigurationClient.DownloadLatestFusionArchiveAsync(
                    apiId,
                    stageName,
                    ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.BadRequest)
            {
                existingArchiveStream = await fusionConfigurationClient
                    .DownloadLatestLegacyFusionArchiveAsync(
                    apiId,
                    stageName,
                    ct);
            }

            var result = await FusionPublishHelpers.ComposeAsync(
                archiveStream,
                existingArchiveStream,
                stageName,
                newSourceSchemas,
                compositionSettings: null,
                console,
                ct);

            if (!result)
            {
                isValid = false;
                return;
            }

            using var archive = FusionArchive.Open(archiveStream);
            await using var schemaStream = await LoadSchemaFile(archive, ct);

            await ValidateSchemaAsync(activity, schemaStream);
        }

        async Task ValidateWithArchive(ICommandLineActivity activity)
        {
            console.Log($"Reading file [blue]{archiveFile.EscapeMarkup()}[/]");

            await using var stream = fileSystem.OpenReadStream(archiveFile);

            Stream schemaStream;
            IDisposable disposableArchive;

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

            try
            {
                await ValidateSchemaAsync(activity, schemaStream);
            }
            finally
            {
                await schemaStream.DisposeAsync();
                disposableArchive.Dispose();
            }
        }

        async Task ValidateSchemaAsync(ICommandLineActivity activity, Stream schemaStream)
        {
            console.Log("Create validation request");

            var requestId = await ValidateAsync(
                console,
                fusionConfigurationClient,
                apiId,
                stageName,
                schemaStream,
                ct);

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            await foreach (var @event in fusionConfigurationClient
                .SubscribeToSchemaVersionValidationUpdatedAsync(requestId, ct))
            {
                switch (@event)
                {
                    case ISchemaVersionValidationFailed v:
                        console.WriteLine("The schema is invalid:");
                        console.PrintMutationErrors(v.Errors);

                        isValid = false;
                        return;

                    case ISchemaVersionValidationSuccess:
                        isValid = true;
                        console.Success("Schema validation succeeded.");
                        return;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("The validation is in progress.");
                        break;

                    default:
                        activity.Update(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }
    }

    private static async Task<string> ValidateAsync(
        IAnsiConsole console,
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
        console.PrintMutationErrorsAndExit(result.Errors);

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
}
