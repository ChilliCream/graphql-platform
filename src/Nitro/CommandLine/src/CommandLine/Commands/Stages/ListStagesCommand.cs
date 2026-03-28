using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class ListStagesCommand : Command
{
    public ListStagesCommand() : base("list")
    {
        Description = "Lists all stages of an API";

        AddOption(Opt<OptionalApiIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IStagesClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IStagesClient client,
        CancellationToken ct)
    {
        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(context, console, client, ct);
        }

        return await RenderNonInteractiveAsync(context, client, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        INitroConsole console,
        IStagesClient client,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to display the clients?";
        var apiId = await context.GetOrPromptForApiIdAsync(apiMessage);

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
            context.SetResult(StageDetailPrompt.From(stage).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IStagesClient client,
        CancellationToken ct)
    {
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw ThrowHelper.Exit("The API ID is required in non-interactive mode.");
        }

        var data = await client.ListStagesAsync(apiId, ct);
        var items = data.Stages
            .Select(x => StageDetailPrompt.From(x).ToObject())
            .ToArray();

        context.SetResult(new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

        return ExitCodes.Success;
    }
}

file static class Extensions
{
    public static string Join(this IEnumerable<string> source, string separator)
        => string.Join(separator, source);
}
