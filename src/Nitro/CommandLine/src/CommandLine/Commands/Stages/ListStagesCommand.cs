using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class ListStagesCommand : Command
{
    public ListStagesCommand(
        INitroConsole console,
        IStagesClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("list")
    {
        Description = "Lists all stages of an API";

        Options.Add(Opt<OptionalApiIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                apisClient,
                sessionService,
                resultHolder,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IStagesClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
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
        const string apiMessage = "For which API do you want to display the clients?";
        var apiId = await parseResult.GetOrPromptForApiIdAsync(apiMessage, console, apisClient, sessionService, ct);

        var stageData = await client.ListStagesAsync(apiId, ct);
        var stages = stageData.Stages;

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
        var apiId = parseResult.GetValue(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw ThrowHelper.Exit("The API ID is required in non-interactive mode.");
        }

        var data = await client.ListStagesAsync(apiId, ct);
        var items = data.Stages
            .Select(x => StageDetailPrompt.From(x).ToObject())
            .ToArray();

        resultHolder.SetResult(new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

        return ExitCodes.Success;
    }
}

file static class Extensions
{
    public static string Join(this IEnumerable<string> source, string separator)
        => string.Join(separator, source);
}
