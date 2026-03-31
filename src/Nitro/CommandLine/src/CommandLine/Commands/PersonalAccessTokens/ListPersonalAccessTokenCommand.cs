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
    public ListPersonalAccessTokenCommand(
        INitroConsole console,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder) : base("list")
    {
        Description = "List all personal access tokens.";

        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(parseResult, console, client, resultHolder, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var container = PaginationContainer
            .CreateConnectionData((after, first, token)
                => client.ListPersonalAccessTokensAsync(after ?? cursor, first, token))
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
            resultHolder.SetResult(new ObjectResult(PersonalAccessTokenDetailPrompt.From(pat).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var data = await client.ListPersonalAccessTokensAsync(cursor, 10, ct);

        var items = data.Items
            .Select(PersonalAccessTokenDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new ObjectResult(new PaginatedListResult<PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult>(items, data.EndCursor)));

        return ExitCodes.Success;
    }
}
