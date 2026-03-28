using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class DeleteClientCommand : Command
{
    public DeleteClientCommand() : base("delete")
    {
        Description = "Deletes a client";

        AddOption(Opt<ForceOption>.Instance);
        AddArgument(Opt<OptionalIdArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IClientsClient>(),
            Opt<OptionalIdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IClientsClient client,
        string? clientId,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Delete a client");
        console.WriteLine();

        const string apiMessage = "For which API do you want to delete a client?";
        const string clientMessage = "Which client do you want to delete?";

        if (clientId is null)
        {
            if (!console.IsHumanReadable())
            {
                throw Exit("The client ID is required in non-interactive mode.");
            }

            var workspaceId = context.RequireWorkspaceId();

            var selectedApi = await SelectApiPrompt
                .New(context.BindingContext.GetRequiredService<IApisClient>(), workspaceId)
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

        var shouldDelete = await context.ConfirmWhenNotForced(
            $"Do you want to delete the client with ID {clientId}?"
                .EscapeMarkup(),
            cancellationToken);

        if (!shouldDelete)
        {
            console.OkLine("Aborted.");
            return ExitCodes.Success;
        }

        var deletedClient = await client.DeleteClientAsync(clientId, cancellationToken);
        console.PrintMutationErrorsAndExit(deletedClient.Errors);

        if (deletedClient.Client is not IClientDetailPrompt_Client clientModel)
        {
            throw Exit("Could not delete the client.");
        }

        console.OkLine($"Client {clientModel.Name.AsHighlight()} was deleted.");

        context.SetResult(ClientDetailPrompt.From(clientModel).ToObject());

        return ExitCodes.Success;
    }
}
