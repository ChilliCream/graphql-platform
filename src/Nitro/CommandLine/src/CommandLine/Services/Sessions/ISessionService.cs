using ChilliCream.Nitro.CommandLine.Output;

namespace ChilliCream.Nitro.CommandLine.Services.Sessions;

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

    /// <summary>
    /// Persists one or more analytical command defaults to the current session.
    /// A <see langword="null"/> argument leaves the corresponding default unchanged,
    /// while a <see cref="SessionDefault{T}.Unset"/> argument clears the default.
    /// Returns the updated session.
    /// </summary>
    Task<Session> SetDefaultsAsync(
        SessionDefault<string>? apiId,
        SessionDefault<string>? stage,
        SessionDefault<OutputFormat>? format,
        CancellationToken cancellationToken);
}

/// <summary>
/// A sentinel wrapper used by <see cref="ISessionService.SetDefaultsAsync"/> to
/// disambiguate between "leave unchanged" (null argument), "clear default" (unset), and
/// "assign a new value" (value).
/// </summary>
internal readonly record struct SessionDefault<T>(T? Value, bool IsUnset)
{
    public static SessionDefault<T> Set(T value) => new(value, false);

    public static SessionDefault<T> Unset => new(default, true);
}
