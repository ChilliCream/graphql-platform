using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ShowClientCommand : Command
{
    public ShowClientCommand() : base("show")
    {
        Description = "Shows details of a client";

        AddArgument(Opt<IdArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IClientsClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IClientsClient client,
        string id,
        CancellationToken cancellationToken)
    {
        var model = await client.ShowClientAsync(id, cancellationToken);

        if (model is IShowClientCommandQuery_Node_Client clientModel)
        {
            context.SetResult(ClientDetailPrompt.From(clientModel).ToObject());
        }
        else
        {
            console.ErrorLine(
                $"Could not find a client with ID {id.EscapeMarkup().AsHighlight()}");
        }

        return ExitCodes.Success;
    }
}
