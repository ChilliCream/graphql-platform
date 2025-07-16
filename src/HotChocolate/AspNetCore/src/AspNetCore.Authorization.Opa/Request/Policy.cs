namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The structure representing information about an OPA policy to be evaluated by the OPA server.
/// </summary>
public sealed class Policy(string path, IReadOnlyList<string> roles)
{
    /// <summary>
    /// Path of the policy. Contains the path string appended to the OPA base address.
    /// </summary>
    public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));

    /// <summary>
    /// Roles associated with the user to evaluate by the policy rule.
    /// </summary>
    public IReadOnlyList<string> Roles { get; } = roles ?? throw new ArgumentNullException(nameof(roles));
}
