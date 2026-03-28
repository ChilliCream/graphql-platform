using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Configuration;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ValidateClientCommand : Command
{
    public ValidateClientCommand(
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem) : base("validate")
    {
        Description = "Validate a client version";

        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);
        Options.Add(Opt<OperationsFileOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(
                console,
                client,
                fileSystem,
                parseResult.GetValue(Opt<StageNameOption>.Instance)!,
                parseResult.GetValue(Opt<ClientIdOption>.Instance)!,
                parseResult.GetValue(Opt<OperationsFileOption>.Instance)!,
                parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance),
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem,
        string stage,
        string clientId,
        string operationsFilePath,
        string? sourceMetadataJson,
        CancellationToken ct)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Validating..."))
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{operationsFilePath.EscapeMarkup()}[/]");

            await using var stream = fileSystem.OpenReadStream(operationsFilePath);

            console.Log("Create validation request");

            var validationRequest = await client.StartClientValidationAsync(
                clientId,
                stage,
                stream,
                source,
                ct);

            console.PrintMutationErrorsAndExit(validationRequest.Errors);
            if (validationRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create validation request!");
            }

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            await foreach (var update in client.SubscribeToClientValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case IClientVersionValidationFailed { Errors: var errors }:
                        console.WriteLine("The client is invalid:");
                        console.PrintMutationErrors(errors);
                        return ExitCodes.Error;

                    case IClientVersionValidationSuccess:
                        console.Success("Client validation succeeded");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("The validation is in progress.");
                        break;

                    default:
                        activity.Update(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }

        return ExitCodes.Error;
    }
}
