using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;

namespace ChilliCream.Nitro.CLI;

internal sealed class ListEnvironmentCommand : Command
{
    public ListEnvironmentCommand() : base("list")
    {
        Description = "Lists all environments of a workspace";

        AddOption(Opt<CursorOption>.Instance);
        AddOption(Opt<WorkspaceIdOption>.Instance);

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
        var workspaceId = context.RequireWorkspaceId();

        if (console.IsHumandReadable())
        {
            return await RenderInteractiveAsync(context, console, client, workspaceId, ct);
        }

        return await RenderNonInteractiveAsync(context, console, client, workspaceId, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .Create((after, first, ct) =>
                    client.ListEnvironmentCommandQuery.ExecuteAsync(workspaceId, after, first, ct),
                static p => p.WorkspaceById?.Environments?.PageInfo,
                static p => p.WorkspaceById?.Environments?.Edges)
            .PageSize(10);

        var environment = await PagedTable
            .From(container)
            .Title("Environments")
            .AddColumn("Id", x => x.Node.Id)
            .AddColumn("Name", x => x.Node.Name)
            .RenderAsync(console, ct);

        if (environment?.Node is IEnvironmentDetailPrompt_Environment node)
        {
            context.SetResult(EnvironmentDetailPrompt.From(node).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var result = await client
            .ListEnvironmentCommandQuery
            .ExecuteAsync(workspaceId, cursor, 10, ct);

        console.EnsureNoErrors(result);

        var endCursor = result.Data?.WorkspaceById?.Environments?.PageInfo.EndCursor;

        var items = result.Data?.WorkspaceById?.Environments?.Edges?.Select(x =>
                new { x.Node.Id, x.Node.Name, })
            .Cast<object>()
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult(items, endCursor!));

        return ExitCodes.Success;
    }
}
