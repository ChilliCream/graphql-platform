using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class ListEnvironmentCommand : Command
{
    public ListEnvironmentCommand() : base("list")
    {
        Description = "List all environments of a workspace.";

        Options.Add(Opt<OptionalCursorOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("environment list");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IEnvironmentsClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(cursor, console, client, resultHolder, workspaceId, ct);
        }

        return await RenderNonInteractiveAsync(cursor, client, resultHolder, workspaceId, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        string? cursor,
        INitroConsole console,
        IEnvironmentsClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .CreateConnectionData((after, first, token)
                => client.ListEnvironmentsAsync(workspaceId, after ?? cursor, first, token))
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
        string? cursor,
        IEnvironmentsClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var data = await client.ListEnvironmentsAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(EnvironmentDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new PaginatedListResult<EnvironmentDetailPrompt.EnvironmentDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
