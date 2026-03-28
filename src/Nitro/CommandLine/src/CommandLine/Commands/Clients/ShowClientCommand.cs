using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ShowClientCommand : Command
{
    public ShowClientCommand(
        INitroConsole console,
        IClientsClient client,
        IResultHolder resultHolder) : base("show")
    {
        Description = "Shows details of a client";

        Arguments.Add(Opt<IdArgument>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var model = await client.ShowClientAsync(id, cancellationToken);

        if (model is IShowClientCommandQuery_Node_Client clientModel)
        {
            resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(clientModel).ToObject()));
        }
        else
        {
            console.ErrorLine(
                $"Could not find a client with ID {id.EscapeMarkup().AsHighlight()}");
        }

        return ExitCodes.Success;
    }
}
