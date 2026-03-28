using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Tests;

internal sealed class TestSessionService : ISessionService
{
    public Session? Session { get; set; }

    public static TestSessionService WithWorkspace(
        string workspaceId = "ws-1",
        string workspaceName = "Workspace")
    {
        return new TestSessionService
        {
            Session = new Session(
                "session-1",
                "subject-1",
                "tenant-1",
                "https://id.chillicream.com",
                "api.chillicream.com",
                "user@chillicream.com",
                tokens: null,
                workspace: new Workspace(workspaceId, workspaceName))
        };
    }

    public Task<Session> LoginAsync(
        string? authority,
        CancellationToken cancellationToken)
        => Task.FromResult(Session!);

    public Task LogoutAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<Session> SelectWorkspaceAsync(
        Workspace workspace,
        CancellationToken cancellationToken)
    {
        Session ??= new Session(
            "session-1",
            "subject-1",
            "tenant-1",
            "https://id.chillicream.com",
            "api.chillicream.com",
            "user@chillicream.com",
            tokens: null,
            workspace: null);

        Session.Workspace = workspace;
        return Task.FromResult(Session);
    }

    public Task<Session?> LoadSessionAsync(CancellationToken cancellationToken)
        => Task.FromResult(Session);
}
