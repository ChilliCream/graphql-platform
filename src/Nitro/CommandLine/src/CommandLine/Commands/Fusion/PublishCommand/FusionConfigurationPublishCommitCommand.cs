using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
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

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IFusionConfigurationClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<IFileSystem>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        ISessionService sessionService,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        var requestId =
            context.ParseResult.GetValueForOption(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, ct) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        var archiveFile =
            context.ParseResult.GetValueForOption(Opt<FusionArchiveFileOption>.Instance)!;

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
