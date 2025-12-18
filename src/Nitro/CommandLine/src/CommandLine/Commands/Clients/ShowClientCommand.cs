using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Client;
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
        AddOption(Opt<ClientDetailFieldsOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<IdArgument>.Instance,
            Opt<ClientDetailFieldsOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string id,
        IEnumerable<string> fields,
        CancellationToken cancellationToken)
    {
        var result = await client.ShowClientCommandQuery.ExecuteAsync(id, cancellationToken);

        var data = result.EnsureData();

        if (data.Node is IClientDetailPrompt_Client node)
        {
            context.SetResult(
                await ClientDetailPrompt.From(node, client).ToObject(fields.ToArray()));
        }
        else
        {
            console.ErrorLine(
                $"Could not find a api with id {id.EscapeMarkup().AsHighlight()}");
        }

        return ExitCodes.Success;
    }
}
