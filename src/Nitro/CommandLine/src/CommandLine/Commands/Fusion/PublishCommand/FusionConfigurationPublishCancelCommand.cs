using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
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
                ErrorMessages.NoFusionRequestId);

        await using (var activity = console.StartActivity(
            "Canceling publication",
            "Failed to cancel the publication."))
        {
            var result = await fusionConfigurationClient.ReleaseDeploymentSlotAsync(requestId, cancellationToken);

            if (result.Errors is { Count: > 0 })
            {
                foreach (var error in result.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IFusionConfigurationRequestNotFoundError err => err.Message,
                        IInvalidProcessingStateTransitionError err => err.Message,
                        IError err => ErrorMessages.UnexpectedMutationError(err),
                        _ => ErrorMessages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                throw new ExitException();
            }

            activity.Success($"Canceled publication for request '{requestId.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }
    }
}
