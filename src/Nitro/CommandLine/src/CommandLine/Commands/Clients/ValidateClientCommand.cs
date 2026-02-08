using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ValidateClientCommand : Command
{
    public ValidateClientCommand() : base("validate")
    {
        Description = "Validate a client version";

        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ClientIdOption>.Instance);
        AddOption(Opt<OperationsFileOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<StageNameOption>.Instance,
            Opt<ClientIdOption>.Instance,
            Opt<OperationsFileOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string stage,
        string clientId,
        FileInfo operationsFile,
        CancellationToken ct)
    {
        console.Title($"Validate to {stage.EscapeMarkup()}");

        var isValid = false;

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Validating...", ValidateClient);
        }
        else
        {
            await ValidateClient(null);
        }

        return isValid ? ExitCodes.Success : ExitCodes.Error;

        async Task ValidateClient(StatusContext? ctx)
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{operationsFile.FullName.EscapeMarkup()}[/]");

            var stream = FileHelpers.CreateFileStream(operationsFile);

            var input = new ValidateClientInput
            {
                ClientId = clientId,
                Stage = stage,
                Operations = new Upload(stream, "operations.graphql")
            };

            console.Log("Create validation request");

            var requestId = await ValidateAsync(console, client, input, ct);

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            using var stopSignal = new Subject<Unit>();

            var subscription = client.OnClientVersionValidationUpdated
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnClientVersionValidationUpdate)
                {
                    case IClientVersionValidationFailed { Errors: var schemaErrors }:
                        console.WriteLine("The client is invalid:");
                        console.PrintErrorsAndExit(schemaErrors);
                        stopSignal.OnNext(Unit.Default);
                        break;

                    case IClientVersionValidationSuccess:
                        isValid = true;
                        stopSignal.OnNext(Unit.Default);
                        console.Success("Client validation succeeded");
                        break;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        ctx?.Status("The validation is in progress.");
                        break;

                    default:
                        ctx?.Status(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }
    }

    private static async Task<string> ValidateAsync(
        IAnsiConsole console,
        IApiClient client,
        ValidateClientInput input,
        CancellationToken ct)
    {
        var result =
            await client.ValidateClientVersion.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.ValidateClient.Errors);

        if (data.ValidateClient.Id is null)
        {
            throw new ExitException("Could not create validation request!");
        }

        return data.ValidateClient.Id;
    }
}
