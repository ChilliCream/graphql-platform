#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

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
        Options.Add(Opt<OptionalFusionArchiveVersionOption>.Instance);
        Options.Add(Opt<OptionalOutputFileOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion download \
              --api-id "<api-id>" \
              --stage "dev" \
              --output-file ./gateway.far
            """);

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

        parseResult.AssertHasAuthentication(sessionService);

        var stageName = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var version = parseResult.GetRequiredValue(Opt<OptionalFusionArchiveVersionOption>.Instance);
        var outputFile = parseResult.GetValue(Opt<OptionalOutputFileOption>.Instance);

        var archiveFormat = version.Major == 1 ? ArchiveFormats.Fgp : ArchiveFormats.Far;
        const string legacyArchiveVersion = "1.0.0";

        if (string.IsNullOrEmpty(outputFile))
        {
            outputFile = "gateway." + archiveFormat;
        }

        if (!Path.IsPathRooted(outputFile))
        {
            outputFile = Path.Combine(fileSystem.GetCurrentDirectory(), outputFile);
        }

        var extension = Path.GetExtension(outputFile);
        var wantsToDownloadFgp = extension.Equals(".fgp", StringComparison.OrdinalIgnoreCase);

        if (wantsToDownloadFgp && version.Major > 1)
        {
            throw new ExitException(
                $"Specify '{OptionalFusionArchiveVersionOption.OptionName} {legacyArchiveVersion}' if you want to download a '.fgp' legacy Fusion archive.");
        }

        if (!wantsToDownloadFgp && version.Major == 1)
        {
            throw new ExitException(
                $"Specify the '.fgp' extension through the '{OptionalOutputFileOption.OptionName}' option, if you want to download a legacy Fusion archive.");
        }

        var isFgp = version.Major == 1;

        await using var stream = await fusionConfigurationClient.DownloadLatestFusionArchiveAsync(
            apiId,
            stageName,
            isFgp ? legacyArchiveVersion : version.ToString(),
            archiveFormat,
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

        console.WriteLine($"Downloaded Fusion configuration to '{outputFile.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
