using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishCancelCommand : Command
{
    public FusionConfigurationPublishCancelCommand() : base("cancel")
    {
        Description = "Cancel a Fusion configuration publish.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("fusion publish cancel");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fusionConfigurationClient = services.GetRequiredService<IFusionConfigurationClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

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
