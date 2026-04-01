using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishValidateCommand : Command
{
    public FusionConfigurationPublishValidateCommand() : base("validate")
    {
        Description = "Validate a Fusion configuration against the schema and clients.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);
        Options.Add(Opt<FusionArchiveFileOption>.Instance);

        this.AddGlobalNitroOptions();

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

        var requestId =
            parseResult.GetValue(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, cancellationToken) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        var archiveFile =
            parseResult.GetValue(Opt<FusionArchiveFileOption>.Instance)!;

        await using (var activity = console.StartActivity(
            "Validating Fusion configuration",
            "Failed to validate the Fusion configuration."))
        {
            return await ValidateAsync(activity);
        }

        async Task<int> ValidateAsync(INitroConsoleActivity activity)
        {
            await using var stream = fileSystem.OpenReadStream(archiveFile);

            var result = await fusionConfigurationClient.ValidateFusionConfigurationPublishAsync(
                requestId,
                stream,
                cancellationToken);

            if (result.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in result.Errors)
                {
                    var errorMessage = error switch
                    {
                        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors_UnauthorizedOperation err => err.Message,
                        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors_FusionConfigurationRequestNotFoundError err => err.Message,
                        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors_InvalidProcessingStateTransitionError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            await foreach (var @event in fusionConfigurationClient
                .SubscribeToFusionConfigurationPublishingTaskChangedAsync(requestId, cancellationToken))
            {
                switch (@event)
                {
                    case IProcessingTaskIsQueued:
                        throw Exit(
                            "Your request is in the queued state. Try to run `fusion-configuration publish start` once the request is ready ");

                    case IFusionConfigurationPublishingFailed:
                        throw Exit("Your request has already failed");

                    case IFusionConfigurationPublishingSuccess:
                        throw Exit("You request is already published");

                    case IProcessingTaskIsReady:
                        throw Exit(
                            "Your request is ready for the composition. Run `fusion-configuration publish start`");

                    case IFusionConfigurationValidationFailed failed:
                        activity.Fail();

                        foreach (var error in failed.Errors)
                        {
                            console.Error.WriteErrorLine(error switch
                            {
                                IUnexpectedProcessingError e => e.Message,
                                IError e => "Unexpected error: " + e.Message,
                                _ => "Unexpected error."
                            });
                        }

                        console.Error.WriteErrorLine("The validation failed.");
                        return ExitCodes.Error;

                    case IFusionConfigurationValidationSuccess:
                        activity.Success("Validated the Fusion configuration.");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                    case IWaitForApproval:
                    case IProcessingTaskApproved:
                        activity.Update("Validating...");
                        break;

                    default:
                        activity.Update("Unknown server response. Consider updating the CLI.", ActivityUpdateKind.Warning);
                        break;
                }
            }

            return ExitCodes.Error;
        }
    }
}
