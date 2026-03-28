using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class DeleteClientCommand : Command
{
    public DeleteClientCommand(
        INitroConsole console,
        IClientsClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("delete")
    {
        Description = "Deletes a client";

        Options.Add(Opt<ForceOption>.Instance);
        Arguments.Add(Opt<OptionalIdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, apisClient, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Delete a client");
        console.WriteLine();

        const string apiMessage = "For which API do you want to delete a client?";
        const string clientMessage = "Which client do you want to delete?";

        var clientId = parseResult.GetValue(Opt<OptionalIdArgument>.Instance);

        if (clientId is null)
        {
            if (!console.IsInteractive)
            {
                throw Exit("The client ID is required in non-interactive mode.");
            }

            var workspaceId = parseResult.GetWorkspaceId(sessionService);

            var selectedApi = await SelectApiPrompt
                .New(apisClient, workspaceId)
                .Title(apiMessage)
                .RenderAsync(console, cancellationToken) ?? throw NoApiSelected();

            var apiId = selectedApi.Id;

            var selectedClient = await SelectClientPrompt
                .New(client, apiId)
                .Title(clientMessage)
                .RenderAsync(console, cancellationToken) ?? throw NoClientSelected();

            console.WriteLine("Selected client: " + selectedClient.Name);

            clientId = selectedClient.Id;
            console.OkQuestion(clientMessage, clientId);
        }
        else
        {
            console.OkQuestion(clientMessage, clientId);
        }

        // TODO: Fix this
        var force = parseResult.GetValue(Opt<ForceOption>.Instance);// is not null;
        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you want to delete the client with ID {clientId}?"
                    .EscapeMarkup(),
                cancellationToken);

            if (!confirmed)
            {
                console.OkLine("Aborted.");
                return ExitCodes.Success;
            }
        }

        var deletedClient = await client.DeleteClientAsync(clientId, cancellationToken);
        console.PrintMutationErrorsAndExit(deletedClient.Errors);

        if (deletedClient.Client is not IClientDetailPrompt_Client clientModel)
        {
            throw Exit("Could not delete the client.");
        }

        console.OkLine($"Client {clientModel.Name.AsHighlight()} was deleted.");

        resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(clientModel).ToObject()));

        return ExitCodes.Success;
    }
}
