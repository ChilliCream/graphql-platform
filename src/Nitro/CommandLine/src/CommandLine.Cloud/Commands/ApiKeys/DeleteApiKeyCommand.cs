using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.ApiKey;

internal sealed class DeleteApiKeyCommand : Command
{
    public DeleteApiKeyCommand() : base("delete")
    {
        Description = "Deletes a api key by id";

        AddArgument(Opt<IdArgument>.Instance);
        AddOption(Opt<ForceOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string keyId,
        CancellationToken cancellationToken)
    {
        var choice = await context.ConfirmWhenNotForced(
            $"Do you really want to delete api key with id {keyId}",
            cancellationToken);

        if (!choice)
        {
            throw Exit("Api key was not deleted");
        }

        var result = await client.DeleteApiKeyCommandMutation
            .ExecuteAsync(new DeleteApiKeyInput { ApiKeyId = keyId }, cancellationToken);

        console.EnsureNoErrors(result);

        var data = console.EnsureData(result);

        console.PrintErrorsAndExit(data.DeleteApiKey.Errors);

        var changeResult = data.DeleteApiKey.ApiKey;
        if (changeResult is null)
        {
            throw Exit("Could not delete api key");
        }

        console.OkLine(
            $"ApiKey {changeResult.Name.AsHighlight()} [dim](ID:{changeResult.Id})[/] was deleted");

        context.SetResult(ApiKeyDetailPrompt.From(changeResult).ToResult());

        return ExitCodes.Success;
    }
}
