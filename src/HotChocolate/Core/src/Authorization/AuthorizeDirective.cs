using System.Text;

namespace HotChocolate.Authorization;

/// <summary>
/// The authorize directive.
/// </summary>
public sealed class AuthorizeDirective
{
    private readonly string _cacheKey;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthorizeDirective"/>.
    /// </summary>
    /// <param name="roles">
    /// The authorization roles.
    /// </param>
    /// <param name="apply">
    /// Specifies when the authorization directive shall be applied.
    /// </param>
    public AuthorizeDirective(
        IReadOnlyList<string> roles,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        : this(null, roles, apply) { }

    /// <summary>
    /// Initializes a new instance of <see cref="AuthorizeDirective"/>.
    /// </summary>
    /// <param name="apply">
    /// Specifies when the authorization directive shall be applied.
    /// </param>
    public AuthorizeDirective(ApplyPolicy apply)
        : this(null, null, apply) { }

    /// <summary>
    /// Initializes a new instance of <see cref="AuthorizeDirective"/>.
    /// </summary>
    /// <param name="policy">
    /// The authorization policy.
    /// </param>
    /// <param name="roles">
    /// The authorization roles.
    /// </param>
    /// <param name="apply">
    /// Specifies when the authorization directive shall be applied.
    /// </param>
    public AuthorizeDirective(
        string? policy = null,
        IReadOnlyList<string>? roles = null,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        Policy = policy;
        Roles = roles?.OrderBy(r => r).ToList();
        Apply = apply;

        _cacheKey = BuildCacheKey(Policy, Roles);
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

    /// <summary>
    /// Gets a cache key that uniquely identifies the combined authorization policy,
    /// of the specified <see cref="Roles"/> and <see cref="Policy"/>.
    /// </summary>
    internal string GetPolicyCacheKey() => _cacheKey;

    private static string BuildCacheKey(string? policy, IReadOnlyList<string>? roles)
    {
        if (string.IsNullOrEmpty(policy) && roles is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(policy))
        {
            sb.Append(policy);
        }

        sb.Append(";");

        if (roles is not null)
        {
            for (var i = 0; i < roles.Count; i++)
            {
                sb.Append(roles[i]);

                if (i < roles.Count - 1)
                {
                    sb.Append(",");
                }
            }
        }

        return sb.ToString();
    }
}
