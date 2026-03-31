using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishCommitCommand : Command
{
    public FusionConfigurationPublishCommitCommand(
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        ISessionService sessionService,
        IFileSystem fileSystem) : base("commit")
    {
        Description = "Commit a Fusion configuration publish.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);
        Options.Add(Opt<FusionArchiveFileOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, (parseResult, cancellationToken)
            => ExecuteAsync(parseResult, console, fusionConfigurationClient, sessionService, fileSystem, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        ISessionService sessionService,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var requestId =
            parseResult.GetValue(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, ct) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        var archiveFile =
            parseResult.GetValue(Opt<FusionArchiveFileOption>.Instance)!;

        var committed = false;

        await using (var activity = console.StartActivity(
            "Publishing Fusion configuration",
            "Failed to publish a new Fusion configuration version."))
        {
            await using var stream = fileSystem.OpenReadStream(archiveFile);
            committed = await FusionPublishHelpers.UploadFusionArchiveAsync(
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
