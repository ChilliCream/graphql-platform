#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO.Compression;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using ChilliCream.Nitro.CommandLine.Options;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

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

        var archiveOption = new FusionArchiveFileOption(isRequired: false);

        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(archiveOption);
        AddOption(Opt<SourceSchemaFileListOption>.Instance);
        this.AddNitroCloudDefaultOptions();

        AddValidator(result =>
        {
            var exclusiveOptionsCount = new[]
            {
                result.FindResultFor(Opt<SourceSchemaFileListOption>.Instance) is not null,
                result.FindResultFor(archiveOption) is not null
            }.Count(x => x);

            if (exclusiveOptionsCount > 1)
            {
                result.ErrorMessage = "You can only specify one of: '--source-schema-file' or '--archive'.";
            }
            else if (exclusiveOptionsCount < 1)
            {
                result.ErrorMessage = "You need to specify one of: '--source-schema-file' or '--archive'.";
            }
        });

        this.SetHandler(
            ExecuteAsync,
            Opt<StageNameOption>.Instance,
            Opt<ApiIdOption>.Instance,
            archiveOption,
            Opt<SourceSchemaFileListOption>.Instance,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<IHttpClientFactory>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        string stageName,
        string apiId,
        string? archiveFile,
        List<string> sourceSchemaFiles,
        IAnsiConsole console,
        IApiClient client,
        IHttpClientFactory httpClientFactory,
        CancellationToken ct)
    {
        console.Title($"Validate against {stageName.EscapeMarkup()}");

        var isValid = false;

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Validating...", async ctx =>
                {
                    if (archiveFile is not null)
                    {
                        await ValidateWithArchive(ctx);
                    }
                    else
                    {
                        await ValidateWithSourceSchemaFiles(ctx);
                    }
                });
        }
        else
        {
            if (archiveFile is not null)
            {
                await ValidateWithArchive(null);
            }
            else
            {
                await ValidateWithSourceSchemaFiles(null);
            }
        }

        return isValid ? ExitCodes.Success : ExitCodes.Error;

        async Task ValidateWithSourceSchemaFiles(StatusContext? ctx)
        {
            var newSourceSchemas = await FusionComposeCommand.ReadSourceSchemasAsync(sourceSchemaFiles, ct);

            var archiveStream = new MemoryStream();
            var existingArchiveStream = await FusionPublishHelpers.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
                client,
                httpClientFactory,
                ct);

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

            await ValidateSchemaAsync(ctx, schemaStream);
        }

        async Task ValidateWithArchive(StatusContext? ctx)
        {
            console.Log($"Reading file [blue]{archiveFile.EscapeMarkup()}[/]");

            await using var stream = FileHelpers.CreateFileStream(new FileInfo(archiveFile));

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
                await ValidateSchemaAsync(ctx, schemaStream);
            }
            finally
            {
                await schemaStream.DisposeAsync();
                disposableArchive.Dispose();
            }
        }

        async Task ValidateSchemaAsync(StatusContext? ctx, Stream schemaStream)
        {
            var input = new ValidateSchemaInput
            {
                ApiId = apiId, Stage = stageName, Schema = new Upload(schemaStream, "schema.graphql")
            };

            console.Log("Create validation request");

            var requestId = await ValidateAsync(console, client, input, ct);

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            using var stopSignal = new Subject<Unit>();

            var subscription = client.OnSchemaVersionValidationUpdated
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnSchemaVersionValidationUpdate)
                {
                    case ISchemaVersionValidationFailed { Errors: var schemaErrors }:
                        console.WriteLine("The schema is invalid:");
                        console.PrintErrorsAndExit(schemaErrors);
                        stopSignal.OnNext(Unit.Default);
                        break;

                    case ISchemaVersionValidationSuccess:
                        isValid = true;
                        stopSignal.OnNext(Unit.Default);

                        console.Success("Schema validation succeeded.");
                        break;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        ctx?.Status("The validation is in progress.");
                        break;

                    default:
                        ctx?.Status(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }
    }

    private static async Task<string> ValidateAsync(
        IAnsiConsole console,
        IApiClient client,
        ValidateSchemaInput input,
        CancellationToken ct)
    {
        var result = await client.ValidateSchemaVersion.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.ValidateSchema.Errors);

        if (data.ValidateSchema.Id is null)
        {
            throw new ExitException("Could not create validation request!");
        }

        return data.ValidateSchema.Id;
    }

    private static async Task<Stream> LoadSchemaFile(FusionArchive archive, CancellationToken ct)
    {
        var latestVersion = await archive.GetLatestSupportedGatewayFormatAsync(ct);
        var configuration = await archive.TryGetGatewayConfigurationAsync(latestVersion, ct);

        if (configuration is null)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve gateway configuration from the fusion archive (format version: {latestVersion}). "
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
