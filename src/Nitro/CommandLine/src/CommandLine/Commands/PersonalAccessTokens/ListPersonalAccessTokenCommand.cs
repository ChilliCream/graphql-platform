using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;

internal sealed class ListPersonalAccessTokenCommand : Command
{
    public ListPersonalAccessTokenCommand() : base("list")
    {
        Description = "List all personal access tokens.";

        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("pat list");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IPersonalAccessTokensClient>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(cursor, console, client, resultHolder, ct);
        }

        return await RenderNonInteractiveAsync(cursor, client, resultHolder, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        string? cursor,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
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
        string? cursor,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var data = await client.ListPersonalAccessTokensAsync(cursor, 10, ct);

        var items = data.Items
            .Select(PersonalAccessTokenDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new PaginatedListResult<PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
