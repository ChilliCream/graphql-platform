using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI.Commands.Client;

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
            Bind.FromServiceProvider<IApiClient>(),
            Opt<OptionalIdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string? clientId,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Delete a client");
        console.WriteLine();

        const string apiMessage = "For which api do you want to delete a client?";
        const string clientMessage = "Which client do you want to delete?";

        if (clientId is null)
        {
            if (!console.IsHumandReadable())
            {
                throw Exit("The client id is required in non-interactive mode.");
            }

            var workspaceId = context.RequireWorkspaceId();

            var selectedApi = await SelectApiPrompt
                .New(client, workspaceId)
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
            $"Do you want to delete the client wih id {clientId}?"
                .EscapeMarkup(),
            cancellationToken);

        if (!shouldDelete)
        {
            console.OkLine("Aborted.");
            return ExitCodes.Success;
        }

        var input = new DeleteClientByIdInput { ClientId = clientId };
        var result =
            await client.DeleteClientByIdCommandMutation.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.DeleteClientById.Errors);

        var createdClient = data.DeleteClientById.Client;
        if (createdClient is null)
        {
            throw Exit("Could not delete the client.");
        }

        console.OkLine($"Client {createdClient.Name.AsHighlight()} was deleted.");

        if (createdClient is IClientDetailPrompt_Client detail)
        {
            context.SetResult(await ClientDetailPrompt.From(detail, client).ToObject([]));
        }

        return ExitCodes.Success;
    }
}
