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
    public ShowClientCommand() : base("show")
    {
        Description = "Show details of a client.";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("client show \"<client-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var client = services.GetRequiredService<IClientsClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var model = await client.GetClientAsync(id, cancellationToken);

        if (model is IShowClientCommandQuery_Node_Client clientModel)
        {
            resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(clientModel).ToObject()));
            return ExitCodes.Success;
        }

        throw Exit($"The client with ID '{id}' was not found.");
    }
}
