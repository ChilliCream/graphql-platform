using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class PublishSchemaCommand : Command
{
    public PublishSchemaCommand() : base("publish")
    {
        Description = "Publish a schema version to a stage";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<ForceOption>.Instance);
        AddOption(Opt<OptionalWaitForApprovalOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            Opt<StageNameOption>.Instance,
            Opt<ApiIdOption>.Instance,
            Opt<ForceOption>.Instance,
            Opt<OptionalWaitForApprovalOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        string stage,
        string apiId,
        bool force,
        bool waitForApproval,
        CancellationToken ct)
    {
        console.Title(
            $"Publish schema with tag {tag.EscapeMarkup()} to {stage.EscapeMarkup()}");

        var committed = false;

        if (console.IsHumanReadable())
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

        return committed ? ExitCodes.Success : ExitCodes.Error;

        async Task PublishSchema(StatusContext? ctx)
        {
            console.Log("Initialized");

            var input = new PublishSchemaInput
            {
                ApiId = apiId,
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

            var requestId = await PublishSchemaAsync(console, client, input, ct);

            console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            using var stopSignal = new Subject<Unit>();

            var subscription = client.OnSchemaVersionPublishUpdated
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnSchemaVersionPublishingUpdate)
                {
                    case IProcessingTaskIsQueued v:
                        ctx?.Status(
                            $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                        break;

                    case ISchemaVersionPublishFailed { Errors: var schemaErrors }:
                        console.WriteLine("Schema publish failed");
                        console.PrintErrorsAndExit(schemaErrors);
                        stopSignal.OnNext(Unit.Default);
                        break;

                    case ISchemaVersionPublishSuccess:
                        committed = true;
                        stopSignal.OnNext(Unit.Default);

                        console.Success("Successfully published schema!");
                        break;

                    case IProcessingTaskIsReady:
                        console.Success("Your request is ready for the committing.");
                        break;

                    case IOperationInProgress:
                        ctx?.Status("The committing of your request is in progress.");
                        break;

                    case IWaitForApproval e:
                        if (e.Deployment is
                            IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Deployment_SchemaDeployment
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

    private static async Task<string> PublishSchemaAsync(
        IAnsiConsole console,
        IApiClient client,
        PublishSchemaInput input,
        CancellationToken ct)
    {
        var result =
            await client.PublishSchemaVersion.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.PublishSchema.Errors);

        if (data.PublishSchema.Id is not { } requestId)
        {
            throw new ExitException("Could not create publish request!");
        }

        return requestId;
    }
}
