using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishValidateCommand : Command
{
    public FusionConfigurationPublishValidateCommand(
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem) : base("validate")
    {
        Description = "Validates a Fusion configuration against the schema and clients.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);
        Options.Add(Opt<FusionArchiveFileOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, (parseResult, cancellationToken)
            => ExecuteAsync(parseResult, console, fusionConfigurationClient, fileSystem, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var requestId =
            parseResult.GetValue(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, cancellationToken) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        var archiveFile =
            parseResult.GetValue(Opt<FusionArchiveFileOption>.Instance)!;

        await using (var activity = console.StartActivity("Validating..."))
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
            console.PrintMutationErrorsAndExit(result.Errors);

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
                        console.WriteLine("The validation failed:");
                        console.PrintMutationErrors(failed.Errors);
                        return ExitCodes.Error;

                    case IFusionConfigurationValidationSuccess:
                        console.Success("The validation was successful");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                    case IWaitForApproval:
                    case IProcessingTaskApproved:
                        activity.Update("The validation is in progress");
                        break;

                    default:
                        throw Exit("Unknown response");
                }
            }

            return ExitCodes.Error;
        }
    }
}
