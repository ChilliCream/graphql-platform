using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class CreateClientCommand : Command
{
    public CreateClientCommand(
        INitroConsole console,
        IClientsClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("create")
    {
        Description = "Creates a new client";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<ClientNameOption>.Instance);

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
        console.WriteLine("Creating a client");
        console.WriteLine();

        const string apiMessage = "For which API do you want to create a client?";
        var apiId = await console.GetOrPromptForApiIdAsync(apiMessage, parseResult, apisClient, sessionService, cancellationToken);

        var name = await console
            .PromptAsync("Name", defaultValue: null, parseResult, Opt<ClientNameOption>.Instance, cancellationToken);

        var data = await client.CreateClientAsync(apiId, name, cancellationToken);
        console.PrintMutationErrorsAndExit(data.Errors);

        if (data.Client is not IClientDetailPrompt_Client createdClient)
        {
            throw ThrowHelper.Exit("Could not create client.");
        }

        console.OkLine($"Client {createdClient.Name.AsHighlight()} created.");

        resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(createdClient).ToObject()));

        return ExitCodes.Success;
    }
}
