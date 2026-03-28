using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients.Components;

internal static class ClientInvocationContextExtensions
{
    public static async Task<string> GetOrSelectClientId(
        this InvocationContext context,
        INitroConsole console,
        IClientsClient client,
        CancellationToken ct)
    {
        var clientId = context.ParseResult.GetValueForOption(Opt<OptionalClientIdOption>.Instance);

        if (clientId is null)
        {
            var apiId = await context.GetOrPromptForApiIdAsync("For which API do you want to list client versions?");

            var selectedClient = await SelectClientPrompt
                .New(client, apiId)
                .Title("Select a client from the list below.")
                .RenderAsync(console, ct) ?? throw ThrowHelper.NoClientSelected();

            clientId = selectedClient.Id;
        }

        return clientId;
    }
}
