using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using static ChilliCream.Nitro.CommandLine.Helpers.Placeholders;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UnpublishClientCommand : Command
{
    public UnpublishClientCommand() : base("unpublish")
    {
        Description = "Unpublish a client version from a stage";

        AddOption(Opt<TagsOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ClientIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IClientsClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IClientsClient client,
        CancellationToken cancellationToken)
    {
        var tags = context.ParseResult.GetValueForOption(Opt<TagsOption>.Instance)?.ToArray()!;
        var stage = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
        var clientId = context.ParseResult.GetValueForOption(Opt<ClientIdOption>.Instance)!;

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
