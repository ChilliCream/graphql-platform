using System.CommandLine.Invocation;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using StrawberryShake;
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
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        var requestId =
            context.ParseResult.GetValueForOption(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(cancellationToken) ??
            throw new ExitException(
                "No request id was provided and no request id was found in the cache. Please provide a request id.");

        var archiveFile =
            context.ParseResult.GetValueForOption(Opt<FusionArchiveFileOption>.Instance)!;

        console.Title("Validating the composition of a fusion configuration");

        if (console.IsHumanReadable())
        {
            return await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Validating...", ValidateAsync);
        }
        else
        {
            return await ValidateAsync(null);
        }

        async Task<int> ValidateAsync(StatusContext? ctx)
        {
            console.Log("Initialized");

            var stream = FileHelpers.CreateFileStream(new FileInfo(archiveFile));

            var input = new ValidateFusionConfigurationCompositionInput
            {
                RequestId = requestId,
                Configuration = new(stream, "gateway.fgp")
            };

            var result = await client.ValidateFusionConfigurationPublish
                .ExecuteAsync(input, cancellationToken);
            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.ValidateFusionConfigurationComposition.Errors);

            using var stopSignal = new Subject<Unit>();
            var subscription = client.OnFusionConfigurationPublishingTaskChanged
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            IFusionConfigurationValidationFailed? failed = null;
            IFusionConfigurationValidationSuccess? success = null;

            await subscription.ForEachAsync(OnNext, cancellationToken);

            if (success is not null)
            {
                console.Success("The validation was successful");
                return ExitCodes.Success;
            }

            console.WriteLine("The validation failed:");
            if (failed is not null)
            {
                console.PrintErrorsAndExit(failed.Errors);
            }

            return ExitCodes.Error;

            void OnNext(IOperationResult<IOnFusionConfigurationPublishingTaskChangedResult> @event)
            {
                if (@event.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (@event.Data?.OnFusionConfigurationPublishingTaskChanged)
                {
                    case IProcessingTaskIsQueued:
                        stopSignal.OnNext(Unit.Default);
                        throw Exit(
                            "Your request is in the queued state. Try to run `fusion-configuration publish start` once the request is ready ");

                    case IFusionConfigurationPublishingFailed:
                        stopSignal.OnNext(Unit.Default);
                        throw Exit("Your request has already failed");

                    case IFusionConfigurationPublishingSuccess:
                        stopSignal.OnNext(Unit.Default);
                        throw Exit("You request is already published");

                    case IProcessingTaskIsReady:
                        stopSignal.OnNext(Unit.Default);
                        throw Exit(
                            "Your request is ready for the composition. Run `fusion-configuration publish start`");

                    case IFusionConfigurationValidationFailed f:
                        stopSignal.OnNext(Unit.Default);
                        failed = f;
                        break;

                    case IFusionConfigurationValidationSuccess s:
                        stopSignal.OnNext(Unit.Default);
                        success = s;
                        break;

                    case IOperationInProgress:
                    case IValidationInProgress:
                    case IWaitForApproval:
                    case IProcessingTaskApproved:
                        ctx?.Status("The validation is in progress");
                        break;

                    default:
                        throw Exit("Unknown response");
                }
            }
        }
    }
}
