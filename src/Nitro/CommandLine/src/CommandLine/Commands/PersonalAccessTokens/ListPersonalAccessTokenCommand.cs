using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;

internal sealed class ListPersonalAccessTokenCommand : Command
{
    public ListPersonalAccessTokenCommand() : base("list")
    {
        Description = "Lists all API keys of a workspace";

        AddOption(Opt<CursorOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IPersonalAccessTokensClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        CancellationToken ct)
    {
        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(context, console, client, ct);
        }

        return await RenderNonInteractiveAsync(context, client, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .CreateConnectionData(client.ListPersonalAccessTokensAsync)
            .PageSize(10);

        var pat = await PagedTable
            .From(container)
            .Title("PersonalAccessTokens")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Description", x => x.Description)
            .AddColumn("Expires in",
                x =>
                {
                    var diff = x.ExpiresAt - DateTimeOffset.UtcNow;

                    if (diff.Days > 0)
                    {
                        return $"{diff.Days} days";
                    }

                    return "[red]Expired[/]";
                })
            .RenderAsync(console, ct);

        if (pat is not null)
        {
            context.SetResult(PersonalAccessTokenDetailPrompt.From(pat).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IPersonalAccessTokensClient client,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var data = await client.ListPersonalAccessTokensAsync(cursor, 10, ct);

        var items = data.Items
            .Select(PersonalAccessTokenDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        context.SetResult(
            new PaginatedListResult<PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult>(
                items,
                data.EndCursor));

        return ExitCodes.Success;
    }
}
