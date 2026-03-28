using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishCommitCommand : Command
{
    public FusionConfigurationPublishCommitCommand(
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem) : base("commit")
    {
        Description = "Commit a Fusion configuration publish.";
        Options.Add(Opt<OptionalRequestIdOption>.Instance);
        Options.Add(Opt<FusionArchiveFileOption>.Instance);

        SetAction((parseResult, cancellationToken)
            => ExecuteAsync(parseResult, console, fusionConfigurationClient, fileSystem, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        var requestId =
            parseResult.GetValue(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, ct) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        var archiveFile =
            parseResult.GetValue(Opt<FusionArchiveFileOption>.Instance)!;

        var committed = false;

        await using (var activity = console.StartActivity("Committing..."))
        {
            await Commit(activity);
        }

        if (!committed)
        {
            throw Exit("The commit has failed.");
        }

        console.Success("Fusion composition was successful.");

        return ExitCodes.Success;

        async Task Commit(INitroConsoleActivity activity)
        {
            await using var stream = fileSystem.OpenReadStream(archiveFile);
            committed = await FusionPublishHelpers.UploadFusionArchiveAsync(
                requestId,
                stream,
                activity,
                console,
                fusionConfigurationClient,
                ct);
        }
    }
}
