using ChilliCream.Nitro.CommandLine.Cloud.Auth;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal interface ISessionService
{
    Session? Session { get; }

    Task<Session> LoginAsync(
        string? authority,
        CancellationToken cancellationToken);

    Task LogoutAsync(CancellationToken cancellationToken);

    Task<Session> SelectWorkspaceAsync(
        Workspace workspace,
        CancellationToken cancellationToken);

    Task<Session?> LoadSessionAsync(CancellationToken cancellationToken);
}
