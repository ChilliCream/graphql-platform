using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Stages;

internal sealed class ListStagesCommand : Command
{
    public ListStagesCommand() : base("list")
    {
        Description = "Lists all stages of an api";

        AddOption(Opt<OptionalApiIdOption>.Instance);

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
        const string apiMessage = "For which api do you want to display the clients?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var result = await client.ListStagesQuery.ExecuteAsync(apiId, ct);
        var data = result.EnsureData();
        var stages = (data.Node as IListStagesQuery_Node_Api)?.Stages;
        if (stages is null)
        {
            throw ThrowHelper.Exit("Could not load stages");
        }

        var stage = await SelectableTable
            .From(stages)
            .Title($"Stages of api {apiId}")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .AddColumn("After",
                x => x.Conditions
                    .OfType<IListStagesQuery_Node_Stages_Conditions_AfterStageCondition>()
                    .Select(x => x.AfterStage!.Name)
                    .Join(","))
            .RenderAsync(console, ct);

        if (stage is IStageDetailPrompt_Stage node)
        {
            context.SetResult(StageDetailPrompt.From(node).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw ThrowHelper.Exit("The api id is required in non-interactive mode.");
        }

        var result = await client.ListStagesQuery.ExecuteAsync(apiId, ct);

        console.EnsureNoErrors(result);

        var items = (result.Data?.Node as IListStagesQuery_Node_Api)?.Stages
            .Select(x => StageDetailPrompt.From(x).ToObject())
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

        return ExitCodes.Success;
    }
}

file static class Extensions
{
    public static string Join(this IEnumerable<string> source, string separator)
        => string.Join(separator, source);
}
