using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class PublishOpenApiCollectionCommand : Command
{
    public PublishOpenApiCollectionCommand() : base("publish")
    {
        Description = "Publish an OpenAPI collection version to an stage";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<OpenApiCollectionIdOption>.Instance);
        AddOption(Opt<ForceOption>.Instance);
        AddOption(Opt<OptionalWaitForApprovalOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            Opt<StageNameOption>.Instance,
            Opt<OpenApiCollectionIdOption>.Instance,
            Opt<ForceOption>.Instance,
            Opt<OptionalWaitForApprovalOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        string stage,
        string openApiCollectionId,
        bool force,
        bool waitForApproval,
        CancellationToken ct)
    {
        console.Title(
            $"Publish OpenAPI collection with tag {tag.EscapeMarkup()} to {stage.EscapeMarkup()}");

        var committed = false;

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Publishing...", PublishOpenApiCollection);
        }
        else
        {
            await PublishOpenApiCollection(null);
        }

        return committed ? ExitCodes.Success : ExitCodes.Error;

        async Task PublishOpenApiCollection(StatusContext? ctx)
        {
            var input = new PublishOpenApiCollectionInput
            {
                OpenApiCollectionId = openApiCollectionId,
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

            var requestId = await PublishOpenApiCollectionAsync(console, client, input, ct);

            console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            using var stopSignal = new Subject<Unit>();

            var subscription = client.PublishOpenApiCollectionCommandSubscription
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnOpenApiCollectionVersionPublishingUpdate)
                {
                    case IProcessingTaskIsQueued v:
                        ctx?.Status(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case IOpenApiCollectionVersionPublishFailed { Errors: var openApiCollectionErrors }:
                        console.WriteLine("OpenAPI collection publish failed");
                        console.PrintErrorsAndExit(openApiCollectionErrors);
                        stopSignal.OnNext(Unit.Default);
                        break;

                    case IOpenApiCollectionVersionPublishSuccess:
                        committed = true;
                        stopSignal.OnNext(Unit.Default);

                        console.Success("Successfully published OpenAPI collection!");
                        break;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for processing.");
                        break;

                    case IOperationInProgress:
                        ctx?.Status("Your request is in progress.");
                        break;

                    case IWaitForApproval e:
                        if (e.Deployment is
                            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_OpenApiCollectionDeployment
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

    private static async Task<string> PublishOpenApiCollectionAsync(
        IAnsiConsole console,
        IApiClient client,
        PublishOpenApiCollectionInput input,
        CancellationToken ct)
    {
        var result =
            await client.PublishOpenApiCollectionCommandMutation.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.PublishOpenApiCollection.Errors);

        if (data.PublishOpenApiCollection.Id is null)
        {
            throw new ExitException("Could not create publish request!");
        }

        return data.PublishOpenApiCollection.Id;
    }
}
