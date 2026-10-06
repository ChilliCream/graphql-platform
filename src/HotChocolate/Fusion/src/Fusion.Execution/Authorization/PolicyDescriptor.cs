using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Describes one occurrence of an authorization directive on a selection together with the
/// policy that evaluates it.
/// </summary>
public sealed class PolicyDescriptor
{
    /// <summary>
    /// Initializes a new instance of <see cref="PolicyDescriptor"/>.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive, see <see cref="DirectiveNames"/>.
    /// </param>
    /// <param name="policyName">
    /// The opaque policy name, or <c>null</c> for directives without one.
    /// </param>
    /// <param name="scopes">
    /// The required scopes as OR-of-AND groups, or a default or empty array for directives
    /// without scopes.
    /// </param>
    /// <param name="selection">
    /// The selection the directive applies to.
    /// </param>
    /// <param name="policy">
    /// The policy that evaluates the directive.
    /// </param>
    public PolicyDescriptor(
        string directiveName,
        string? policyName,
        ImmutableArray<ImmutableArray<string>> scopes,
        ISelection selection,
        IPolicy policy)
    {
        ArgumentException.ThrowIfNullOrEmpty(directiveName);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(policy);

        DirectiveName = directiveName;
        PolicyName = policyName;
        Scopes = scopes.IsDefault ? [] : scopes;
        Selection = selection;
        Policy = policy;
    }

    /// <summary>
    /// Gets the name of the directive.
    /// </summary>
    public string DirectiveName { get; }

    /// <summary>
    /// Gets the opaque policy name, or <c>null</c> for directives without one.
    /// </summary>
    public string? PolicyName { get; }

    /// <summary>
    /// Gets the required scopes as OR-of-AND groups, which is empty when the descriptor has no
    /// scope requirement.
    /// </summary>
    public ImmutableArray<ImmutableArray<string>> Scopes { get; }

    /// <summary>
    /// Gets the selection the directive applies to.
    /// </summary>
    public ISelection Selection { get; }

    /// <summary>
    /// Gets the policy that evaluates the directive.
    /// </summary>
    public IPolicy Policy { get; }
}
