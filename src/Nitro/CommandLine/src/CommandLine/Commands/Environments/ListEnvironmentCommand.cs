using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
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
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IEnvironmentsClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IEnvironmentsClient client,
        CancellationToken ct)
    {
        var workspaceId = context.RequireWorkspaceId();

        if (console.IsInteractive())
        {
            return await RenderInteractiveAsync(context, console, client, workspaceId, ct);
        }

        return await RenderNonInteractiveAsync(context, client, workspaceId, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        INitroConsole console,
        IEnvironmentsClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .CreateConnectionData((after, first, token)
                => client.ListEnvironmentsAsync(workspaceId, after, first, token))
            .PageSize(10);

        var environment = await PagedTable
            .From(container)
            .Title("Environments")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .RenderAsync(console, ct);

        if (environment is not null)
        {
            context.SetResult(EnvironmentDetailPrompt.From(environment).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IEnvironmentsClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var data = await client.ListEnvironmentsAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(EnvironmentDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        context.SetResult(new PaginatedListResult<EnvironmentDetailPrompt.EnvironmentDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
