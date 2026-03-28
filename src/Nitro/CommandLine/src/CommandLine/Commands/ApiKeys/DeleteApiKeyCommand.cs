using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class DeleteApiKeyCommand : Command
{
    public DeleteApiKeyCommand() : base("delete")
    {
        Description = "Deletes an API key by ID";

        AddArgument(Opt<IdArgument>.Instance);
        AddOption(Opt<ForceOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IApiKeysClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IApiKeysClient client,
        string keyId,
        CancellationToken cancellationToken)
    {
        var choice = await context.ConfirmWhenNotForced(
            $"Do you really want to delete API key with ID {keyId}",
            cancellationToken);

        if (!choice)
        {
            throw Exit("API key was not deleted");
        }

        var data = await client.DeleteApiKeyAsync(keyId, cancellationToken);
        // console.PrintMutationErrorsAndExit(data.Errors);

        if (data.ApiKey is not { } key)
        {
            throw Exit("Could not delete API key");
        }

        // console.OkLine(
        //     $"API key {key.Name.AsHighlight()} [dim](ID: {key.Id})[/] was deleted");

        context.SetResult(ApiKeyDetailPrompt.From(key).ToObject());

        return ExitCodes.Success;
    }
}
