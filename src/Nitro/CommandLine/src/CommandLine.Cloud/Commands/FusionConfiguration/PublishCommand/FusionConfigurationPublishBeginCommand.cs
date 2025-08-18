using System.CommandLine.Invocation;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.FusionConfiguration;

internal sealed class FusionConfigurationPublishBeginCommand : Command
{
    public FusionConfigurationPublishBeginCommand() : base("begin")
    {
        Description = "Begin a configuration publish. This command will request a deployment slot";
        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<OptionalSubgraphIdOption>.Instance);
        AddOption(Opt<OptionalSubgraphNameOption>.Instance);
        AddOption(Opt<OptionalWaitForApprovalOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<IConfigurationService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        IConfigurationService configurationService,
        CancellationToken ct)
    {
        var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
        var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
        var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;
        var subgraphId =
            context.ParseResult.GetValueForOption(Opt<OptionalSubgraphIdOption>.Instance)!;
        var subgraphName =
            context.ParseResult.GetValueForOption(Opt<OptionalSubgraphNameOption>.Instance)!;
        var waitForApproval =
            context.ParseResult.GetValueForOption(Opt<OptionalWaitForApprovalOption>.Instance);

        console.Title("Begin a configuration publish");

        if (console.IsHumandReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Publishing...", PublishSchema);
        }
        else
        {
            await PublishSchema(null);
        }

        return ExitCodes.Success;

        async Task PublishSchema(StatusContext? ctx)
        {
            console.Log("Initialized");

            var input = new BeginFusionConfigurationPublishInput()
            {
                ApiId = apiId,
                Tag = tag,
                StageName = stageName,
                SubgraphName = subgraphName,
                SubgraphApiId = subgraphId,
                WaitForApproval = waitForApproval
            };

            var result = await client.BeginFusionConfigurationPublish.ExecuteAsync(input, ct);
            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.BeginFusionConfigurationPublish.Errors);
            if (data.BeginFusionConfigurationPublish.RequestId is not { } requestId)
            {
                throw Exit("No request id returned");
            }

            console.MarkupLine($"Your request id is [blue]{requestId}[/]");

            using var stopSignal = new Subject<Unit>();
            var subscription = client.OnFusionConfigurationPublishingTaskChanged
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await subscription.ForEachAsync(OnNext, ct);

            void OnNext(IOperationResult<IOnFusionConfigurationPublishingTaskChangedResult> x)
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
                            $"Your request is queued and is in position [blue]{v.QueuePosition}[/]");
                        break;

                    case IFusionConfigurationPublishingFailed v:
                        stopSignal.OnNext(Unit.Default);
                        console.PrintErrorsAndExit(v.Errors);
                        throw Exit("Your request has failed");

                    case IFusionConfigurationPublishingSuccess:
                        stopSignal.OnNext(Unit.Default);
                        console.WarningLine("Your request is already published");
                        break;

                    case IProcessingTaskIsReady:
                        stopSignal.OnNext(Unit.Default);
                        console.Success("Your request is ready for the composition");
                        break;

                    case IFusionConfigurationValidationFailed:
                    case IFusionConfigurationValidationSuccess:
                    case IValidationInProgress:
                    case IOperationInProgress:
                    case IWaitForApproval:
                    case IProcessingTaskApproved:
                        stopSignal.OnNext(Unit.Default);
                        console.Success("Your request is already processing");
                        break;

                    default:
                        throw Exit("Unknown response");
                }
            }

            console.WriteLine("Request ID:");
            console.WriteLine(requestId);
            context.SetResult(new { RequestId = requestId });
            await FusionConfigurationPublishingState.SetRequestId(requestId, ct);
        }
    }
}
