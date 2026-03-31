#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionDownloadCommand : Command
{
    public FusionDownloadCommand() : base("download")
    {
        Description = "Download the most recent gateway configuration.";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OptionalOutputFileOption>.Instance);
        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fusionConfigurationClient = services.GetRequiredService<IFusionConfigurationClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var stageName = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var outputFile =
            parseResult.GetValue(Opt<OptionalOutputFileOption>.Instance) ??
            Path.Combine(Environment.CurrentDirectory, "gateway.far");

        var isFgp = Path.GetExtension(outputFile).Equals(".fgp", StringComparison.OrdinalIgnoreCase);

        await using (var activity = console.StartActivity(
            $"Downloading latest Fusion configuration from stage '{stageName.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'",
            "Failed to download the latest Fusion configuration."))
        {
            await using var stream = isFgp
                ? await fusionConfigurationClient.DownloadLatestLegacyFusionArchiveAsync(
                    apiId,
                    stageName,
                    cancellationToken)
                : await fusionConfigurationClient.DownloadLatestFusionArchiveAsync(
                    apiId,
                    stageName,
                    WellKnownVersions.LatestGatewayFormatVersion.ToString(),
                    cancellationToken);

            if (stream is null)
            {
                throw new ExitException("The API with the given ID does not exist or does not have a download URL.");
            }

            if (fileSystem.FileExists(outputFile))
            {
                fileSystem.DeleteFile(outputFile);
            }

            await using var fileStream = fileSystem.CreateFile(outputFile);

            await stream.CopyToAsync(fileStream, cancellationToken);

            activity.Success($"Downloaded Fusion configuration from stage '{stageName.EscapeMarkup()}'.");

            if (!console.IsHumanReadable)
            {
                resultHolder.SetResult(new ObjectResult(new FusionDownloadResult
                {
                    File = outputFile
                }));
            }

            return ExitCodes.Success;
        }
    }

    public class FusionDownloadResult
    {
        public required string File { get; init; }
    }
}
