using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class DeleteApiKeyCommand : Command
{
    public DeleteApiKeyCommand(
        INitroConsole console,
        IApiKeysClient apiKeysClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("delete")
    {
        Description = "Delete an API key by ID.";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<ForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                apiKeysClient,
                sessionService,
                resultHolder,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApiKeysClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var keyId = parseResult.GetValue(Opt<IdArgument>.Instance) ?? throw Exit("An API key ID is required.");
        var force = parseResult.GetValue(Opt<ForceOption>.Instance);

        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you really want to delete API key with ID '{keyId}'?",
                cancellationToken);

            if (!confirmed)
            {
                throw Exit("API key was not deleted.");
            }
        }

        await using (var activity = console.StartActivity(
            $"Deleting API key '{keyId.EscapeMarkup()}'",
            "Failed to delete the API key."))
        {
            var data = await client.DeleteApiKeyAsync(keyId, cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IApiKeyNotFoundError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            if (data.ApiKey is not { } key)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Deleted API key '{keyId.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(ApiKeyDetailPrompt.From(key).ToObject()));

            return ExitCodes.Success;
        }
    }
}
