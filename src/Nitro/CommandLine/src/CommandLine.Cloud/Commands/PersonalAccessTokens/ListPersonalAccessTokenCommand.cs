using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ListPersonalAccessTokenCommand : Command
{
    public ListPersonalAccessTokenCommand() : base("list")
    {
        Description = "Lists all api keys of a workspace";

        AddOption(Opt<CursorOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        if (console.IsHumandReadable())
        {
            return await RenderInteractiveAsync(context, console, client, ct);
        }

        return await RenderNonInteractiveAsync(context, console, client, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .Create((after, first, _) =>
                    client.ListPersonalAccessTokenCommandQuery.ExecuteAsync(after, first, ct),
                static p => p.Me?.PersonalAccessTokens?.PageInfo,
                static p => p.Me?.PersonalAccessTokens?.Edges)
            .PageSize(10);

        var pats = await PagedTable
            .From(container)
            .Title("PersonalAccessTokens")
            .AddColumn("Id", x => x.Node.Id)
            .AddColumn("Description", x => x.Node.Description)
            .AddColumn("Expires in",
                x =>
                {
                    var diff = x.Node.ExpiresAt - DateTimeOffset.UtcNow;

                    if (diff.Days > 0)
                    {
                        return $"{diff.Days} days";
                    }

                    return "[red]Expired[/]";
                })
            .RenderAsync(console, ct);

        if (pats?.Node is IPersonalAccessTokenDetailPrompt_PersonalAccessToken node)
        {
            context.SetResult(PersonalAccessTokenDetailPrompt.From(node).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var result = await client
            .ListPersonalAccessTokenCommandQuery
            .ExecuteAsync(cursor, 10, ct);

        console.EnsureNoErrors(result);

        var endCursor = result.Data?.Me?.PersonalAccessTokens?.PageInfo.EndCursor;

        var items = result.Data?.Me?.PersonalAccessTokens?.Edges?.Select(x =>
                    PersonalAccessTokenDetailPrompt.From(x.Node).ToObject())
                .ToArray() ??
            [];

        context.SetResult(
            new PaginatedListResult<PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult>(items,
                endCursor));

        return ExitCodes.Success;
    }
}
