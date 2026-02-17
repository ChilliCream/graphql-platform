using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class PublishMcpFeatureCollectionCommand : Command
{
    public PublishMcpFeatureCollectionCommand() : base("publish")
    {
        Description = "Publish an MCP Feature Collection version to a stage";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<McpFeatureCollectionIdOption>.Instance);
        AddOption(Opt<ForceOption>.Instance);
        AddOption(Opt<OptionalWaitForApprovalOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            Opt<StageNameOption>.Instance,
            Opt<McpFeatureCollectionIdOption>.Instance,
            Opt<ForceOption>.Instance,
            Opt<OptionalWaitForApprovalOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        string stage,
        string mcpFeatureCollectionId,
        bool force,
        bool waitForApproval,
        CancellationToken ct)
    {
        console.Title(
            $"Publish MCP Feature Collection with tag {tag.EscapeMarkup()} to {stage.EscapeMarkup()}");

        var committed = false;

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Publishing...", PublishMcpFeatureCollection);
        }
        else
        {
            await PublishMcpFeatureCollection(null);
        }

        return committed ? ExitCodes.Success : ExitCodes.Error;

        async Task PublishMcpFeatureCollection(StatusContext? ctx)
        {
            var input = new PublishMcpFeatureCollectionInput
            {
                McpFeatureCollectionId = mcpFeatureCollectionId,
                Stage = stage,
                Tag = tag,
                WaitForApproval = waitForApproval
            };

            if (force)
            {
                input = input with { Force = true };
                console.Log("[yellow]Force push is enabled[/]");
            }

            console.Log("Create publish request");

            var requestId = await PublishMcpFeatureCollectionAsync(console, client, input, ct);

            console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            using var stopSignal = new Subject<Unit>();

            var subscription = client.PublishMcpFeatureCollectionCommandSubscription
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnMcpFeatureCollectionVersionPublishingUpdate)
                {
                    case IProcessingTaskIsQueued v:
                        ctx?.Status(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case IMcpFeatureCollectionVersionPublishFailed { Errors: var mcpFeatureCollectionErrors }:
                        console.ErrorLine("MCP Feature Collection publish failed");
                        console.PrintErrorsAndExit(mcpFeatureCollectionErrors);
                        stopSignal.OnNext(Unit.Default);
                        break;

                    case IMcpFeatureCollectionVersionPublishSuccess:
                        committed = true;
                        stopSignal.OnNext(Unit.Default);

                        console.Success("Successfully published MCP Feature Collection!");
                        break;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for processing.");
                        break;

                    case IOperationInProgress:
                        ctx?.Status("Your request is in progress.");
                        break;

                    case IWaitForApproval e:
                        if (e.Deployment is
                            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_McpFeatureCollectionDeployment
                            deployment)
                        {
                            console.PrintErrors(deployment.Errors);
                        }

                        ctx?.Status(
                            "The processing of your request is waiting for approval. Check Nitro to approve the request.");
                        break;

                    case IProcessingTaskApproved:
                        ctx?.Status("The processing of your request is approved.");

                        break;

                    default:
                        ctx?.Status(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }
    }

    private static async Task<string> PublishMcpFeatureCollectionAsync(
        IAnsiConsole console,
        IApiClient client,
        PublishMcpFeatureCollectionInput input,
        CancellationToken ct)
    {
        var result =
            await client.PublishMcpFeatureCollectionCommandMutation.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.PublishMcpFeatureCollection.Errors);

        if (data.PublishMcpFeatureCollection.Id is null)
        {
            throw new ExitException("Could not create publish request!");
        }

        return data.PublishMcpFeatureCollection.Id;
    }
}
