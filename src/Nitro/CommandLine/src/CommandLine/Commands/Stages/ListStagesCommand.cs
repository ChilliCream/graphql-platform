using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class ListStagesCommand : Command
{
    public ListStagesCommand() : base("list")
    {
        Description = "List all stages of an API.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("stage list --api-id \"<api-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IStagesClient>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(parseResult, console, client, apisClient, sessionService, resultHolder, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IStagesClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var apiId = await console.GetOrPromptForApiIdAsync("For which API do you want to display the clients?", parseResult, apisClient, sessionService, ct);

        var stages = await client.ListStagesAsync(apiId, ct) ?? [];

        var stage = await SelectableTable
            .From(stages)
            .Title($"Stages of API {apiId}")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .AddColumn("After",
                x => x.Conditions
                    .OfType<IAfterStageCondition>()
                    .Select(y => y.AfterStage?.Name)
                    .OfType<string>()
                    .Join(","))
            .RenderAsync(console, ct);

        if (stage is not null)
        {
            resultHolder.SetResult(new ObjectResult(StageDetailPrompt.From(stage).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IStagesClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var apiId = parseResult.GetRequiredOptionalValue(Opt<OptionalApiIdOption>.Instance);

        var data = await client.ListStagesAsync(apiId, ct) ?? [];
        var items = data
            .Select(x => StageDetailPrompt.From(x).ToObject())
            .ToArray();

        resultHolder.SetResult(
            new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

        return ExitCodes.Success;
    }
}

file static class Extensions
{
    public static string Join(this IEnumerable<string> source, string separator)
        => string.Join(separator, source);
}
