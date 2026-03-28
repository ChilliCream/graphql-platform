using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishValidateCommand : Command
{
    public FusionConfigurationPublishValidateCommand() : base("validate")
    {
        Description = "Validates a Fusion configuration against the schema and clients.";
        AddOption(Opt<OptionalRequestIdOption>.Instance);
        AddOption(Opt<FusionArchiveFileOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IFusionConfigurationClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<IFileSystem>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        ISessionService sessionService,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var requestId =
            context.ParseResult.GetValueForOption(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, cancellationToken) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        var archiveFile =
            context.ParseResult.GetValueForOption(Opt<FusionArchiveFileOption>.Instance)!;

        await using (var activity = console.StartActivity("Validating..."))
        {
            return await ValidateAsync(activity);
        }

        async Task<int> ValidateAsync(ICommandLineActivity activity)
        {
            console.Log("Initialized");

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
