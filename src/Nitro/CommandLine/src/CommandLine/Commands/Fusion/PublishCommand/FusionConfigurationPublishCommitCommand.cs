using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishCommitCommand : Command
{
    public FusionConfigurationPublishCommitCommand() : base("commit")
    {
        Description = "Commit a Fusion configuration publish.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);
        Options.Add(Opt<FusionArchiveFileOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("fusion publish commit --archive ./gateway.far");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fusionConfigurationClient = services.GetRequiredService<IFusionConfigurationClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        parseResult.AssertHasAuthentication(sessionService);

        var requestId =
            parseResult.GetValue(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, ct) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        var archiveFile =
            parseResult.GetRequiredValue(Opt<FusionArchiveFileOption>.Instance);

        if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(fileSystem.GetCurrentDirectory(), archiveFile);
        }

        if (!fileSystem.FileExists(archiveFile))
        {
            throw new ExitException(ErrorMessages.ArchiveFileDoesNotExist(archiveFile));
        }

        var committed = false;

        await using (var activity = console.StartActivity(
            "Publishing Fusion configuration",
            "Failed to publish a new Fusion configuration version."))
        {
            await using var stream = fileSystem.OpenReadStream(archiveFile);
            committed = await FusionPublishHelpers.UploadFusionConfigurationAsync(
                requestId,
                stream,
                activity,
                console,
                fusionConfigurationClient,
                ct);

            if (committed)
            {
                activity.Success("Published Fusion configuration.");
            }
            else
            {
                activity.Fail();
            }
        }

        if (!committed)
        {
            throw Exit("The commit has failed.");
        }

        return ExitCodes.Success;
    }
}
