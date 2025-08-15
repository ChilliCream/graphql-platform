using System.CommandLine.Invocation;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Helpers;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CommandLine;
using StrawberryShake;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI.Commands.FusionConfiguration;

internal sealed class FusionConfigurationPublishCommitCommand : Command
{
    public FusionConfigurationPublishCommitCommand() : base("commit")
    {
        Description = "Commit a fusion configuration publish.";
        AddOption(Opt<OptionalRequestIdOption>.Instance);
        AddOption(Opt<ConfigurationFileOption>.Instance);

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
        CancellationToken ct)
    {
        var requestId =
            context.ParseResult.GetValueForOption(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(ct) ??
            throw new ExitException(
                "No request id was provided and no request id was found in the cache. Please provide a request id.");

        var configurationFile =
            context.ParseResult.GetValueForOption(Opt<ConfigurationFileOption>.Instance)!;

        console.Title("Commit the composition of a fusion configuration");

        var committed = false;

        if (console.IsHumandReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Committing...", Commit);
        }
        else
        {
            await Commit(null);
        }

        if (!committed)
        {
            throw Exit("The commit has failed.");
        }

        return ExitCodes.Success;

        async Task Commit(StatusContext? ctx)
        {
            await CommitAsync(console, client, ct, requestId, configurationFile);

            using var stopSignal = new Subject<Unit>();

            var subscription = client.OnFusionConfigurationPublishingTaskChanged
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnFusionConfigurationPublishingTaskChanged)
                {
                    case IProcessingTaskIsQueued v:
                        ctx?.Status(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case IFusionConfigurationPublishingFailed v:
                        stopSignal.OnNext(Unit.Default);
                        console.PrintErrorsAndExit(v.Errors);
                        throw Exit("The commit has failed.");

                    case IFusionConfigurationPublishingSuccess:
                        committed = true;
                        stopSignal.OnNext(Unit.Default);

                        console.Success("Fusion composition was successful.");
                        break;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for the committing.");
                        break;

                    case IFusionConfigurationValidationFailed:
                        ctx?.Status(
                            "The validation of your request has failed. Check the errors in Nitro.");
                        break;

                    case IFusionConfigurationValidationSuccess:
                        ctx?.Status("The validation of your request was successful.");
                        break;

                    case IValidationInProgress:
                        ctx?.Status("The validation of your request is in progress.");
                        break;

                    case IOperationInProgress:
                        ctx?.Status("The committing of your request is in progress.");
                        break;

                    case IWaitForApproval e:
                        if (e.Deployment is
                            IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Deployment_FusionConfigurationDeployment
                            deployment)
                        {
                            console.PrintErrors(deployment.Errors);
                        }

                        ctx?.Status(
                            "The committing of your request is waiting for approval. Check Nitro to approve the request.");
                        break;

                    case IProcessingTaskApproved:
                        ctx?.Status("The committing of your request is approved.");

                        break;

                    default:
                        ctx?.Status(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }
    }

    private static async Task CommitAsync(
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct,
        string requestId,
        FileInfo configurationFile)
    {
        var stream = FileHelpers.CreateFileStream(configurationFile);
        var input = new CommitFusionConfigurationPublishInput()
        {
            RequestId = requestId,
            Configuration = new(stream, "gateway.fgp")
        };

        var result =
            await client.CommitFusionConfigurationPublish.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.CommitFusionConfigurationPublish.Errors);
    }
}
