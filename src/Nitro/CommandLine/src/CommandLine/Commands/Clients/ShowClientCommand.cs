using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ShowClientCommand : Command
{
    public ShowClientCommand(
        INitroConsole console,
        IClientsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("show")
    {
        Description = "Shows details of a client";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        IClientsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var model = await client.ShowClientAsync(id, cancellationToken);

        if (model is IShowClientCommandQuery_Node_Client clientModel)
        {
            resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(clientModel).ToObject()));
            return ExitCodes.Success;
        }

        throw Exit($"The client with ID '{id}' was not found.");
    }
}
