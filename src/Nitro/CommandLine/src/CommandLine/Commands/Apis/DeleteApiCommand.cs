using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class DeleteApiCommand : Command
{
    public DeleteApiCommand() : base("delete")
    {
        Description = "Delete an API by ID.";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("api delete \"<api-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var apiId = parseResult.GetRequiredValue(Opt<IdArgument>.Instance);

        var apiResult = await client.GetApiForDeleteAsync(apiId, cancellationToken);
        if (apiResult is not IDeleteApiCommandQuery_Node_Api { Name: { } apiName })
        {
            throw Exit($"The API with ID '{apiId}' was not found.");
        }

        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you really want to delete API {apiName.AsHighlight()}",
                cancellationToken);

            if (!confirmed)
            {
                throw Exit("The API was not deleted.");
            }
        }

        await using var activity = console.StartActivity(
            $"Deleting API '{apiId.EscapeMarkup()}'",
            "Failed to delete the API.");

        var data = await client.DeleteApiAsync(apiId, cancellationToken);
        if (data.Errors?.Count > 0)
        {
            activity.Fail();

            foreach (var mutationError in data.Errors)
            {
                var errorMessage = mutationError switch
                {
                    IError err => "Unexpected mutation error: " + err.Message,
                    _ => "Unexpected mutation error."
                };

                console.Error.WriteErrorLine(errorMessage);
                return ExitCodes.Error;
            }
        }

        if (data.Api is not IApiDetailPrompt_Api api)
        {
            throw MutationReturnedNoData();
        }

        activity.Success($"Deleted API '{apiId.EscapeMarkup()}'.");

        resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(api).ToObject()));

        return ExitCodes.Success;
    }
}
