using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishCancelCommand : Command
{
    public FusionConfigurationPublishCancelCommand(
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        ISessionService sessionService,
        IFileSystem fileSystem) : base("cancel")
    {
        Description = "Cancels a Fusion configuration publish.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);

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
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var requestId =
            parseResult.GetValue(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, cancellationToken) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        await using (var activity = console.StartActivity(
            "Canceling publication",
            "Failed to cancel the publication."))
        {
            await fusionConfigurationClient.ReleaseDeploymentSlotAsync(requestId, cancellationToken);

            activity.Success($"Canceled publication for request '{requestId.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }
    }
}
