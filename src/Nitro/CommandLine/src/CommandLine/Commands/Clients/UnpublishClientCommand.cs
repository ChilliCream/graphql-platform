using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using static ChilliCream.Nitro.CommandLine.Helpers.Placeholders;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UnpublishClientCommand : Command
{
    public UnpublishClientCommand(
        INitroConsole console,
        IClientsClient client) : base("unpublish")
    {
        Description = "Unpublish a client version from a stage";

        Options.Add(Opt<TagsOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        CancellationToken cancellationToken)
    {
        var tags = parseResult.GetValue(Opt<TagsOption>.Instance)?.ToArray()!;
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var clientId = parseResult.GetValue(Opt<ClientIdOption>.Instance)!;

        var title = tags.Length > 1
            ? $"Unpublish clients with tags {string.Join(", ", tags).EscapeMarkup()} from {stage.EscapeMarkup()}"
            : $"Unpublish client with tag {tags[0].EscapeMarkup()} from {stage.EscapeMarkup()}";

        await using (var activity = console.StartActivity("Unpublishing..."))
        {
            foreach (var tag in tags)
            {
                activity.Update($"Unpublishing {tag.EscapeMarkup()}...");

                var result = await client.UnpublishClientVersionAsync(
                    clientId,
                    stage,
                    tag,
                    cancellationToken);

                console.PrintMutationErrorsAndExit(result.Errors);

                var clientName = result.ClientVersion?.Client?.Name ?? NotFound;

                console.Success(
                    $"Unpublished [bold]{clientName.EscapeMarkup()}:{tag.EscapeMarkup()}[/] from {stage.EscapeMarkup()} ");
            }
        }

        return ExitCodes.Success;
    }
}
