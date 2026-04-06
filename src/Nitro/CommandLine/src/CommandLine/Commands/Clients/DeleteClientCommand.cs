using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class DeleteClientCommand : Command
{
    public DeleteClientCommand() : base("delete")
    {
        Description = "Delete a client.";

        Arguments.Add(Opt<OptionalIdArgument>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("client delete \"<client-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IClientsClient>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

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

        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
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

        await using (var activity = console.StartActivity(
            $"Deleting client '{clientId.EscapeMarkup()}'",
            "Failed to delete the client."))
        {
            var deletedClient = await client.DeleteClientAsync(clientId, cancellationToken);

            if (deletedClient.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in deletedClient.Errors)
                {
                    var errorMessage = error switch
                    {
                        IDeleteClientByIdCommandMutation_DeleteClientById_Errors_ClientNotFoundError err => err.Message,
                        IDeleteClientByIdCommandMutation_DeleteClientById_Errors_UnauthorizedOperation err => err.Message,
                        IError err => ErrorMessages.UnexpectedMutationError(err),
                        _ => ErrorMessages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            if (deletedClient.Client is not IClientDetailPrompt_Client clientModel)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Deleted client '{clientId.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(clientModel).ToObject()));

            return ExitCodes.Success;
        }
    }
}
