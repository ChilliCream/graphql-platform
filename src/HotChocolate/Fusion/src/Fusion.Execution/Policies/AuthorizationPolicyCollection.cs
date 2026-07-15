using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Provides access to the authorization policies owned by a Fusion schema.
/// </summary>
public sealed class AuthorizationPolicyCollection
    : IReadOnlyList<IAuthorizationPolicy>
{
    private readonly IAuthorizationPolicy[] _policies;
    private readonly FrozenDictionary<string, IAuthorizationPolicy> _policyLookup;

    internal AuthorizationPolicyCollection(IEnumerable<IAuthorizationPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        _policies = policies.ToArray();
        var policyLookup = new Dictionary<string, IAuthorizationPolicy>(_policies.Length, StringComparer.Ordinal);

        for (var i = 0; i < _policies.Length; i++)
        {
            var policy = _policies[i];
            ArgumentNullException.ThrowIfNull(policy);
            var name = policy.Name;

            if (string.IsNullOrEmpty(name))
            {
                throw ThrowHelper.AuthorizationPolicyNameEmpty();
            }

            if (!policyLookup.TryAdd(name, policy))
            {
                throw ThrowHelper.AuthorizationPolicyNameDuplicate(name);
            }

            _policies[i] = policy;
        }

        _policyLookup = policyLookup.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the number of authorization policies in the collection.
    /// </summary>
    public int Count => _policies.Length;

    /// <summary>
    /// Gets the authorization policy at the specified index.
    /// </summary>
    public IAuthorizationPolicy this[int index] => _policies[index];

    /// <summary>
    /// Gets the authorization policy with the specified name.
    /// </summary>
    public IAuthorizationPolicy this[string name] => Get(name);

    /// <summary>
    /// Gets the authorization policy with the specified name.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// The collection does not contain a policy with the specified name.
    /// </exception>
    public IAuthorizationPolicy Get(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_policyLookup.TryGetValue(name, out var policy))
        {
            return policy;
        }

        throw ThrowHelper.AuthorizationPolicyNotFound(name);
    }

    /// <summary>
    /// Tries to get the authorization policy with the specified name.
    /// </summary>
    public bool TryGet(
        string name,
        [NotNullWhen(true)] out IAuthorizationPolicy? policy)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _policyLookup.TryGetValue(name, out policy);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the authorization policies.
    /// </summary>
    public IEnumerator<IAuthorizationPolicy> GetEnumerator()
        => ((IEnumerable<IAuthorizationPolicy>)_policies).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _policies.GetEnumerator();

    internal static AuthorizationPolicyCollection Empty { get; } = new([]);
}
