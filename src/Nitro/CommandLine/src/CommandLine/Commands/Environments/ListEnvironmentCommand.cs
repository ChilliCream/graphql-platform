using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

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

        if (console.IsHumanReadable())
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
                EnvironmentDetailPrompt.From(x.Node).ToObject())
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult<EnvironmentDetailPrompt.EnvironmentDetailPromptResult>(items, endCursor));

        return ExitCodes.Success;
    }
}
