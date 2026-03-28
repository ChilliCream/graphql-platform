using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class ListEnvironmentCommand : Command
{
    public ListEnvironmentCommand(
        INitroConsole console,
        IEnvironmentsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("list")
    {
        Description = "Lists all environments of a workspace";

        Options.Add(Opt<CursorOption>.Instance);
        Options.Add(Opt<WorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IEnvironmentsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(console, client, resultHolder, workspaceId, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, workspaceId, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        INitroConsole console,
        IEnvironmentsClient client,
        IResultHolder resultHolder,
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
            resultHolder.SetResult(new ObjectResult(EnvironmentDetailPrompt.From(environment).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IEnvironmentsClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = parseResult.GetValue(Opt<CursorOption>.Instance);
        var data = await client.ListEnvironmentsAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(EnvironmentDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(new ObjectResult(
            new PaginatedListResult<EnvironmentDetailPrompt.EnvironmentDetailPromptResult>(items, data.EndCursor)));

        return ExitCodes.Success;
    }
}
