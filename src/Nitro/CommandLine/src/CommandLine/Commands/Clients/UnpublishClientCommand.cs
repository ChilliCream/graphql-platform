using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.Helpers.Placeholders;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UnpublishClientCommand : Command
{
    public UnpublishClientCommand(
        INitroConsole console,
        IClientsClient client,
        ISessionService sessionService) : base("unpublish")
    {
        Description = "Unpublish a client version from a stage";

        Options.Add(Opt<TagsOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, sessionService, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var tags = parseResult.GetValue(Opt<TagsOption>.Instance)?.ToArray()!;
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var clientId = parseResult.GetValue(Opt<ClientIdOption>.Instance)!;

        await using (var activity = console.StartActivity(
            $"Unpublishing client '{clientId.EscapeMarkup()}' from stage '{stage.EscapeMarkup()}'",
            "Failed to unpublish the client."))
        {
            foreach (var tag in tags)
            {
                activity.Update($"Unpublishing {tag.EscapeMarkup()}...");

                var result = await client.UnpublishClientVersionAsync(
                    clientId,
                    stage,
                    tag,
                    cancellationToken);

                if (result.Errors?.Count > 0)
                {
                    activity.Fail();

                    foreach (var error in result.Errors)
                    {
                        var errorMessage = error switch
                        {
                            IConcurrentOperationError err => err.Message,
                            IStageNotFoundError err => err.Message,
                            IClientVersionNotFoundError err => err.Message,
                            IUnauthorizedOperation err => err.Message,
                            IClientNotFoundError err => err.Message,
                            IError err => "Unexpected mutation error: " + err.Message,
                            _ => "Unexpected mutation error."
                        };

                        console.Error.WriteErrorLine(errorMessage);
                    }

                    return ExitCodes.Error;
                }

                var clientName = result.ClientVersion?.Client?.Name ?? NotFound;

                console.Success(
                    $"Unpublished [bold]{clientName.EscapeMarkup()}:{tag.EscapeMarkup()}[/] from {stage.EscapeMarkup()} ");
            }

            activity.Success($"Unpublished client '{clientId.EscapeMarkup()}' from stage '{stage.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }
}
