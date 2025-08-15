using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine;
using static ChilliCream.Nitro.CommandLine.Cloud.Helpers.Placeholders;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class UnpublishClientCommand : Command
{
    public UnpublishClientCommand() : base("unpublish")
    {
        Description = "Unpublish a client version from an stage";

        AddOption(Opt<TagsOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ClientIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken cancellationToken)
    {
        var tags = context.ParseResult.GetValueForOption(Opt<TagsOption>.Instance)?.ToArray()!;
        var stage = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
        var clientId = context.ParseResult.GetValueForOption(Opt<ClientIdOption>.Instance)!;

        var title = tags.Length > 1
            ? $"Unpublish clients with tags {string.Join(", ", tags).EscapeMarkup()} from {stage.EscapeMarkup()}"
            : $"Unpublish client with tag {tags[0].EscapeMarkup()} from {stage.EscapeMarkup()}";

        console.Title(title);

        if (console.IsHumandReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Unpublishing...", UnpublishClient);
        }
        else
        {
            await UnpublishClient(null);
        }

        return ExitCodes.Success;

        async Task UnpublishClient(StatusContext? ctx)
        {
            foreach (var tag in tags)
            {
                ctx?.Status($"Unpublishing {tag.EscapeMarkup()}...");

                var input =
                    new UnpublishClientInput { ClientId = clientId, Stage = stage, Tag = tag };

                var result =
                    await client.UnpublishClient.ExecuteAsync(input, cancellationToken);

                console.EnsureNoErrors(result);
                var data = console.EnsureData(result);
                console.PrintErrorsAndExit(data.UnpublishClient.Errors);

                if (data.UnpublishClient.ClientVersion is not
                    {
                        Client: var requestedClient
                    })
                {
                    throw new ExitException("Could not unpublish client!");
                }

                var clientName = requestedClient?.Name ?? NotFound;

                console.Success(
                    $"Unpublished [bold]{clientName.EscapeMarkup()}:{tag.EscapeMarkup()}[/] from {stage.EscapeMarkup()} ");
            }
        }
    }
}
