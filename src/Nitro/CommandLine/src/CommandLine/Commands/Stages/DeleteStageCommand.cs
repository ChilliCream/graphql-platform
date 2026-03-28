using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class DeleteStageCommand : Command
{
    public DeleteStageCommand() : base("delete")
    {
        Description = "Deletes a stage by name";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ForceOption>.Instance);

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
        CancellationToken cancellationToken)
    {
        const string apiMessage = "For which API do you want to force delete a stage?";
        var apiId = await context.GetOrPromptForApiIdAsync(apiMessage);

        var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;

        var shouldDelete = await context.ConfirmWhenNotForced(
            $"Do you really want to force delete stage {stageName.AsHighlight()}",
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

        context.SetResult(new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

        console.OkLine($"Stage {stageName.AsHighlight()} was force deleted");

        return ExitCodes.Success;
    }
}
