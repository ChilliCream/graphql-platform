using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.Helpers.Placeholders;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UnpublishClientCommand : Command
{
    public UnpublishClientCommand() : base("unpublish")
    {
        Description = "Unpublish a client version from a stage.";

        Options.Add(Opt<ClientTagsToUnpublishOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            client unpublish \
              --client-id "<client-id>" \
              --stage "dev" \
              --tag "v1"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IClientsClient>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var tags = parseResult.GetRequiredValue(Opt<ClientTagsToUnpublishOption>.Instance).ToArray();
        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var clientId = parseResult.GetRequiredValue(Opt<ClientIdOption>.Instance);

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
                            IError err => ErrorMessages.UnexpectedMutationError(err),
                            _ => ErrorMessages.UnexpectedMutationError()
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
