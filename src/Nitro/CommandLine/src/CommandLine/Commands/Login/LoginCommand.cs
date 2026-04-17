using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Login;

internal sealed class LoginCommand : Command
{
    public LoginCommand() : base("login")
    {
        Description =
            "Log in interactively through your default browser";

        Options.Add(Opt<IdentityCloudUrlOption>.Instance);
        Arguments.Add(Opt<IdentityCloudUrlArgument>.Instance);

        this.AddExamples("login");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IWorkspacesClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var clientContext = services.GetRequiredService<NitroClientContext>();

        if (!console.IsInteractive)
        {
            throw new ExitException(
                "`nitro login` requires an interactive console. "
                + $"Use '{OptionalApiKeyOption.OptionName}' to authenticate command invocations in non-interactive environments.");
        }

        var cloudUrl = parseResult.GetValue(Opt<IdentityCloudUrlOption>.Instance);
        var url = parseResult.GetValue(Opt<IdentityCloudUrlArgument>.Instance);

        url ??= cloudUrl;

        await using (var activity = console.StartActivity("Logging in via browser", "Failed to log in."))
        {
            activity.Update($"Browser opened at {url.EscapeMarkup()}. Continue login there.");

            var session = await sessionService.LoginAsync(url, cancellationToken);
            if (session is null)
            {
                throw new ExitException("There was a failure and Nitro could not log you in.");
            }

            clientContext.Configure(
                session.ApiUrl,
                new NitroClientAccessTokenAuthorization(session.Tokens!.AccessToken));

            var page = await client.SelectWorkspacesAsync(null, 5, cancellationToken);
            var email = session.Email.EscapeMarkup();

            if (page.Items.Count == 0)
            {
                activity.Update(
                    "You do not have any workspaces. Run `nitro launch` and create one.",
                    ActivityUpdateKind.Warning);
                activity.Success($"Logged in as [green]{email}[/]");
                return ExitCodes.Success;
            }

            if (page.Items.Count == 1)
            {
                var only = page.Items[0];
                await sessionService.SelectWorkspaceAsync(
                    new Workspace(only.Id, only.Name),
                    cancellationToken);
                activity.Success(
                    $"Logged in as [green]{email}[/] (Workspace: [green]{only.Name.EscapeMarkup()}[/])");
                return ExitCodes.Success;
            }

            activity.Success($"Logged in as [green]{email}[/]");
        }

        var paginationContainer = PaginationContainer.CreateConnectionData(client.SelectWorkspacesAsync);
        var selected = await PagedSelectionPrompt
            .New(paginationContainer)
            .Title("Which workspace do you want to use as your default?".AsQuestion())
            .UseConverter(x => x.Name)
            .RenderAsync(console, cancellationToken);

        if (selected is null)
        {
            throw new ExitException("No workspace was selected as default.");
        }

        await sessionService.SelectWorkspaceAsync(
            new Workspace(selected.Id, selected.Name),
            cancellationToken);

        console.MarkupLine($"(Workspace: [green]{selected.Name.EscapeMarkup()}[/])");

        return ExitCodes.Success;
    }
}
