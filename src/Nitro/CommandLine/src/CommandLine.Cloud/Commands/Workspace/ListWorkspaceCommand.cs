using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ListWorkspaceCommand : Command
{
    public ListWorkspaceCommand() : base("list")
    {
        Description = "Lists all workspaces";

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
            .Create(
                client.ListWorkspaceCommandQuery.ExecuteAsync,
                static p => p.Me?.Workspaces?.PageInfo,
                static p => p.Me?.Workspaces?.Edges)
            .PageSize(10);

        var workspace = await PagedTable
            .From(container)
            .Title("Workspaces")
            .AddColumn("Id", x => x.Node.Id)
            .AddColumn("Name", x => x.Node.Name)
            .AddColumn("IsPersonal", x => x.Node.Personal.AsIcon())
            .RenderAsync(console, ct);

        if (workspace?.Node is IWorkspaceDetailPrompt_Workspace node)
        {
            context.SetResult(WorkspaceDetailPrompt.From(node).ToObject());
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
            .ListWorkspaceCommandQuery
            .ExecuteAsync(cursor, 10, ct);

        console.EnsureNoErrors(result);

        var endCursor = result.Data?.Me?.Workspaces?.PageInfo.EndCursor;

        var items = result.Data?.Me?.Workspaces?.Edges?.Select(x =>
                WorkspaceDetailPrompt.From(x.Node).ToObject())
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult(items, endCursor!));

        return ExitCodes.Success;
    }
}
