using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class DeleteStageCommand : Command
{
    public DeleteStageCommand(
        INitroConsole console,
        IStagesClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("delete")
    {
        Description = "Deletes a stage by name";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ForceOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
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
        CancellationToken cancellationToken)
    {
        const string apiMessage = "For which API do you want to force delete a stage?";
        var apiId = await parseResult.GetOrPromptForApiIdAsync(
            apiMessage,
            console,
            apisClient,
            sessionService,
            cancellationToken);

        var stageName = parseResult.GetValue(Opt<StageNameOption>.Instance)!;

        var shouldDelete = await parseResult.ConfirmWhenNotForced(
            $"Do you really want to force delete stage {stageName.AsHighlight()}",
            console,
            cancellationToken);

        if (!shouldDelete)
        {
            throw Exit("Stage was not deleted");
        }

        var data = await client.ForceDeleteStageAsync(apiId, stageName, cancellationToken);
        console.PrintMutationErrorsAndExit(data.Errors);

        if (data.Api is null)
        {
            throw Exit("Could not delete the stage");
        }

        var items = data.Api.Stages
            .Select(x => StageDetailPrompt.From(x).ToObject())
            .ToArray();

        resultHolder.SetResult(new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

        console.OkLine($"Stage {stageName.AsHighlight()} was force deleted");

        return ExitCodes.Success;
    }
}
