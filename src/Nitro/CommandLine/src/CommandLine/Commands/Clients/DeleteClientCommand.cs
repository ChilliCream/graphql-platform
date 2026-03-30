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
        parseResult.AssertHasAuthentication(sessionService);

        const string clientMessage = "Which client do you want to delete?";

        var clientId = parseResult.GetValue(Opt<OptionalIdArgument>.Instance);

        if (clientId is null)
        {
            if (!console.IsInteractive)
            {
                throw MissingRequiredOption("id");
            }

            var workspaceId = parseResult.GetWorkspaceId(sessionService);

            var selectedApi = await SelectApiPrompt
                .New(apisClient, workspaceId)
                .Title("For which API do you want to delete a client?")
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

        var force = parseResult.GetValue(Opt<ForceOption>.Instance);
        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you want to delete the client with ID {clientId}?".EscapeMarkup(),
                cancellationToken);

            if (!confirmed)
            {
                console.OkLine("Aborted.");
                return ExitCodes.Success;
            }
        }

        await using (var activity = console.StartActivity($"Deleting client '{clientId.EscapeMarkup()}'"))
        {
            var deletedClient = await client.DeleteClientAsync(clientId, cancellationToken);

            if (deletedClient.Errors?.Count > 0)
            {
                activity.Fail("Failed to delete the client.");

                foreach (var error in deletedClient.Errors)
                {
                    var errorMessage = error switch
                    {
                        IDeleteClientByIdCommandMutation_DeleteClientById_Errors_ClientNotFoundError err => err.Message,
                        IDeleteClientByIdCommandMutation_DeleteClientById_Errors_UnauthorizedOperation err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            if (deletedClient.Client is not IClientDetailPrompt_Client clientModel)
            {
                activity.Fail("Failed to delete the client.");
                console.Error.WriteErrorLine("Could not delete the client.");
                return ExitCodes.Error;
            }

            activity.Success($"Deleted client '{clientId.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(clientModel).ToObject()));

            return ExitCodes.Success;
        }
    }
}
