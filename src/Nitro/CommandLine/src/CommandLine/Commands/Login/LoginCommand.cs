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
        url = NormalizeIdentityUrl(url);

        var session = await sessionService.LoginAsync(
            url,
            (loginUrl, browserOpened) =>
            {
                var escapedUrl = loginUrl.EscapeMarkup();
                console.MarkupLine(
                    browserOpened
                        ? $"Continue the login in your browser. If it does not open, use this URL: {escapedUrl}"
                        : $"{Glyphs.ExclamationMark.Space()}Could not open a browser. Use this URL to log in: {escapedUrl}");
            },
            cancellationToken);

        clientContext.Configure(
            session.ApiUrl,
            new NitroClientAccessTokenAuthorization(session.Tokens!.AccessToken));

        var page = await client.SelectWorkspacesAsync(null, 5, cancellationToken);
        var loggedIn = $"Logged in as [green]{session.Email.EscapeMarkup()}[/]";

        if (page.Items.Count == 0)
        {
            console.OkLine(loggedIn);
            console.MarkupLine(
                $"{Glyphs.ExclamationMark.Space()}You do not have any workspaces. Run `nitro launch` and create one.");
            return ExitCodes.Success;
        }

        Workspace workspace;

        if (page.Items.Count == 1)
        {
            var only = page.Items[0];
            workspace = new Workspace(only.Id, only.Name);
        }
        else
        {
            var paginationContainer = PaginationContainer.CreateConnectionData(client.SelectWorkspacesAsync);
            var selected = await PagedSelectionPrompt
                .New(paginationContainer)
                .Title(Prompts.SelectDefaultWorkspace.AsQuestion())
                .UseConverter(x => x.Name)
                .RenderAsync(console, cancellationToken);

            if (selected is null)
            {
                console.OkLine(loggedIn);
                throw new ExitException("No workspace was selected as default.");
            }

            workspace = new Workspace(selected.Id, selected.Name);
        }

        await sessionService.SelectWorkspaceAsync(workspace, cancellationToken);
        console.OkLine($"{loggedIn} (Workspace: [green]{workspace.Name.EscapeMarkup()}[/])");

        return ExitCodes.Success;
    }

    private static string? NormalizeIdentityUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        if (!url.StartsWith("https://") && !url.StartsWith("http://"))
        {
            url = $"https://{url}";
        }

        var uriBuilder = new UriBuilder(url)
        {
            Path = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty,
            UserName = string.Empty,
            Password = string.Empty
        };

        return uriBuilder.Uri.GetLeftPart(UriPartial.Authority);
    }
}
