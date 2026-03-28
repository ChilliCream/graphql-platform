using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class CreateClientCommand : Command
{
    public CreateClientCommand() : base("create")
    {
        Description = "Creates a new client";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<ClientNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IClientsClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IClientsClient client,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Creating a client");
        console.WriteLine();

        const string apiMessage = "For which API do you want to create a client?";
        var apiId = await context.GetOrPromptForApiIdAsync(apiMessage);

        var name = await context
            .OptionOrAskAsync("Name", Opt<ClientNameOption>.Instance, cancellationToken);

        var data = await client.CreateClientAsync(apiId, name, cancellationToken);
        console.PrintMutationErrorsAndExit(data.Errors);

        if (data.Client is not IClientDetailPrompt_Client createdClient)
        {
            throw ThrowHelper.Exit("Could not create client.");
        }

        console.OkLine($"Client {createdClient.Name.AsHighlight()} created.");

        context.SetResult(ClientDetailPrompt.From(createdClient).ToObject());

        return ExitCodes.Success;
    }
}
