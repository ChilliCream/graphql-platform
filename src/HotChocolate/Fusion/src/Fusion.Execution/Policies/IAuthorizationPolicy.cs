using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents an authorization policy owned by a Fusion schema.
/// </summary>
/// <remarks>
/// <see cref="Name"/> and <see cref="Requirements"/> must remain stable for the schema lifetime.
/// <see cref="EvaluateAsync"/> may be called concurrently.
/// </remarks>
public interface IAuthorizationPolicy
{
    string Name { get; }

    /// <summary>
    /// Gets the graph data required to evaluate this policy.
    /// </summary>
    /// <remarks>
    /// When this property is <c>null</c>, the policy produces a target-independent decision.
    /// The policy is evaluated lazily at most once per request, and its decision is reused for
    /// every application of the policy that is reached during that request.
    /// Such a policy must not derive its decision from target-specific context or entity data.
    /// </remarks>
    SelectionSetNode? Requirements { get; }

    ValueTask EvaluateAsync(
        IAuthorizationContext context,
        EntityData entities,
        CancellationToken cancellationToken = default);
}

public interface IAuthorizationContext : IFeatureProvider
{
    ISelection? Selection { get; }

    ITypeDefinition Type { get; }

    PolicyDenialBehavior OnDenied { get; }

    ClaimsPrincipal User { get; }

    void Deny(int index, string? reason = null);
}

public readonly struct EntityData
{
    private readonly CompositeResultElement[] _entities;
    private readonly int _entitiesCount;

    public EntityData(CompositeResultElement[] entities, int count)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, entities.Length);

        _entities = entities;
        _entitiesCount = count;
    }

    public int Count => _entitiesCount;

    public CompositeResultElement this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_entitiesCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _entities[index];
        }
    }

    public ReadOnlySpan<CompositeResultElement> Entities
        => _entities is null ? [] : new(_entities, 0, _entitiesCount);
}
