using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
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

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ForceOption>.Instance);

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
        CancellationToken cancellationToken)
    {
        const string apiMessage = "For which API do you want to force delete a stage?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;

        var shouldDelete = await context.ConfirmWhenNotForced(
            $"Do you really want to force delete stage {stageName.AsHighlight()}",
            cancellationToken);

        if (!shouldDelete)
        {
            throw Exit("Stage was not deleted");
        }

        var input = new ForceDeleteStageByIdInput { ApiId = apiId, StageName = stageName };
        var result = await client.ForceDeleteStageByIdCommandMutation
            .ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.ForceDeleteStageById.Errors);

        var stages = data.ForceDeleteStageById.Api?.Stages;
        if (stages is null)
        {
            throw Exit("Could not delete the stage");
        }

        var items = stages
            .Select(x => StageDetailPrompt.From(x).ToObject())
            .ToArray();

        context.SetResult(new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

        console.OkLine($"Stage {stageName.AsHighlight()} was force deleted");

        return ExitCodes.Success;
    }
}
