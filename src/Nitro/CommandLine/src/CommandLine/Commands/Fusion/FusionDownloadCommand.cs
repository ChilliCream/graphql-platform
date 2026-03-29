#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionDownloadCommand : Command
{
    public FusionDownloadCommand(
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem,
        ISessionService sessionService) : base("download")
    {
        Description = "Downloads the most recent gateway configuration";

        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<OptionalOutputFileOption>.Instance);
        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, fusionConfigurationClient, fileSystem, sessionService, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var stageName = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var outputFile =
            parseResult.GetValue(Opt<OptionalOutputFileOption>.Instance) ??
            Path.Combine(Environment.CurrentDirectory, "gateway.far");

        var isFgp = Path.GetExtension(outputFile).Equals(".fgp", StringComparison.OrdinalIgnoreCase);

        await using var stream = isFgp
            ? await fusionConfigurationClient.DownloadLatestLegacyFusionArchiveAsync(
                apiId,
                stageName,
                cancellationToken)
            : await fusionConfigurationClient.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
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

        console.MarkupLine($"Downloaded Fusion configuration to: {outputFile}");

        return ExitCodes.Success;
    }
}
