using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Stages;

internal sealed class DeleteStageCommand : Command
{
    public DeleteStageCommand() : base("delete")
    {
        Description = "Delete a stage by name.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            stage delete \
              --stage "dev" \
              --api-id "<api-id>"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IStagesClient>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        const string apiMessage = "For which API do you want to force delete a stage?";
        var apiId = await parseResult.GetOrPromptForApiIdAsync(
            apiMessage,
            console,
            apisClient,
            sessionService,
            cancellationToken);

        var stageName = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);

        var shouldDelete = await parseResult.ConfirmWhenNotForced(
            $"Do you really want to force delete stage {stageName.AsHighlight()}",
            console,
            cancellationToken);

        if (!shouldDelete)
        {
            throw Exit("Stage was not deleted.");
        }

        await using (var activity = console.StartActivity(
            $"Deleting stage '{stageName.EscapeMarkup()}' from API '{apiId.EscapeMarkup()}'",
            "Failed to delete the stage."))
        {
            var data = await client.ForceDeleteStageAsync(apiId, stageName, cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IApiNotFoundError err => err.Message,
                        IStageNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IError err => ErrorMessages.UnexpectedMutationError(err),
                        _ => ErrorMessages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (data.Api is null)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Deleted stage '{stageName.EscapeMarkup()}'.");

            console.WriteLine();

            var items = data.Api.Stages
                .Select(x => StageDetailPrompt.From(x).ToObject())
                .ToArray();

            resultHolder.SetResult(
                new PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>(items, null));

            return ExitCodes.Success;
        }
    }
}
