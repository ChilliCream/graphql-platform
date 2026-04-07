using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishStartCommand : Command
{
    public FusionConfigurationPublishStartCommand() : base("start")
    {
        Description = "Start a Fusion configuration publish.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("fusion publish start");

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
                Messages.NoFusionRequestId);

        await using (var activity = console.StartActivity(
            "Starting composition",
            "Failed to start the composition."))
        {
            await fusionConfigurationClient.ClaimDeploymentSlotAsync(requestId, cancellationToken);

            activity.Success($"Started composition for request '{requestId.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }
    }
}
