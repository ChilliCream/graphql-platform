using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class DeleteApiKeyCommand : Command
{
    public DeleteApiKeyCommand(
        INitroConsole console,
        IApiKeysClient apiKeysClient,
        IResultHolder resultHolder) : base("delete")
    {
        Description = "Deletes an API key by ID";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<ForceOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, apiKeysClient, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApiKeysClient client,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        var keyId = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        // TODO: Fix
        var force = parseResult.GetValue(Opt<ForceOption>.Instance); // is not null;
        bool choice;
        if (force)
        {
            choice = true;
        }
        else
        {
            choice = await console.ConfirmAsync(
                $"Do you really want to delete API key with ID {keyId}",
                cancellationToken);
        }

        if (!choice)
        {
            throw Exit("API key was not deleted");
        }

        var data = await client.DeleteApiKeyAsync(keyId, cancellationToken);
        console.PrintMutationErrorsAndExit(data.Errors);

        if (data.ApiKey is not { } key)
        {
            throw Exit("Could not delete API key");
        }

        console.OkLine(
            $"API key {key.Name.AsHighlight()} [dim](ID: {key.Id})[/] was deleted");

        resultHolder.SetResult(new ObjectResult(ApiKeyDetailPrompt.From(key).ToObject()));

        return ExitCodes.Success;
    }
}
