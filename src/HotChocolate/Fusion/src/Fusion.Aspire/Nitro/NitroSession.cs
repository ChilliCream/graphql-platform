namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// A read-only view of the session file that the Nitro CLI writes when a user signs in with
/// <c>nitro login</c>. Only the members that the Aspire integration needs are modeled, all other
/// members of the file are ignored. The integration never writes this file.
/// </summary>
internal sealed class NitroSession
{
    /// <summary>
    /// Gets the Nitro API URL that the session was created for. The CLI stores this value
    /// without a scheme, so it has to be normalized before it is used.
    /// </summary>
    public string? ApiUrl { get; init; }

    /// <summary>
    /// Gets the email address of the signed-in user.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the tokens of the session.
    /// </summary>
    public NitroSessionTokens? Tokens { get; init; }

    /// <summary>
    /// Gets the workspace that the signed-in user selected.
    /// </summary>
    public NitroSessionWorkspace? Workspace { get; init; }
}

/// <summary>
/// The tokens of a Nitro CLI session.
/// </summary>
internal sealed class NitroSessionTokens
{
    /// <summary>
    /// Gets the access token that authorizes requests against the Nitro API.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Gets the point in time at which <see cref="AccessToken"/> stops being accepted.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// The workspace that the signed-in user selected in the Nitro CLI.
/// </summary>
internal sealed class NitroSessionWorkspace
{
    /// <summary>
    /// Gets the id of the workspace.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the name of the workspace.
    /// </summary>
    public string? Name { get; init; }
}
