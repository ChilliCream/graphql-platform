using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Helpers;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;

namespace ChilliCream.Nitro.CLI;

internal sealed class ShowClientCommand : Command
{
    public ShowClientCommand() : base("show")
    {
        Description = "Shows details of an client";

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
