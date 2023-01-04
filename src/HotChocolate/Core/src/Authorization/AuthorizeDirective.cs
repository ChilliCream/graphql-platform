using System.Collections.Generic;

namespace HotChocolate.Authorization;

public sealed class AuthorizeDirective
{
    public AuthorizeDirective(
        IReadOnlyList<string> roles,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        : this(null, roles, apply) { }

    public AuthorizeDirective(
        string? policy = null,
        IReadOnlyList<string>? roles = null,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        Policy = policy;
        Roles = roles;
        Apply = apply;
    }

    /// <summary>
    /// Gets the policy name that determines access to the resource.
    /// </summary>
    public string? Policy { get; }

    /// <summary>
    /// Gets of roles that are allowed to access the resource.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; }

    /// <summary>
    /// <para>
    /// Gets a value indicating if the resolver has to be executed
    /// before the policy is run or after the policy is run.
    /// </para>
    /// <para>
    /// The before policy option is good if the actual object is needed
    /// for the policy to be evaluated.
    /// </para>
    /// <para>The default is BeforeResolver.</para>
    /// </summary>
    public ApplyPolicy Apply { get; }
}
