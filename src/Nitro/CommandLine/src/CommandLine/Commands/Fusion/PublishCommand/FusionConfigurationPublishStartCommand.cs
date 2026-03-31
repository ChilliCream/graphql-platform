using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishStartCommand : Command
{
    public FusionConfigurationPublishStartCommand() : base("start")
    {
        Description = "Start a Fusion configuration publish.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(async (services, parseResult, cancellationToken) =>
        {
            var console = services.GetRequiredService<INitroConsole>();
            var fusionConfigurationClient = services.GetRequiredService<IFusionConfigurationClient>();
            var sessionService = services.GetRequiredService<ISessionService>();
            var fileSystem = services.GetRequiredService<IFileSystem>();
            return await ExecuteAsync(parseResult, console, fusionConfigurationClient, sessionService, fileSystem, cancellationToken);
        });
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
            "Starting composition",
            "Failed to start the composition."))
        {
            await fusionConfigurationClient.ClaimDeploymentSlotAsync(requestId, cancellationToken);

            activity.Success($"Started composition for request '{requestId.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }
    }
}
