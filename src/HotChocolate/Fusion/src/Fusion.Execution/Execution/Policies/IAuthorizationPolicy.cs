using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

public interface IAuthorizationPolicy
{
    string Name { get; }

    SelectionSetNode? Requirements { get; }

    ValueTask EvaluateAsync(
        IAuthorizationContext context,
        EntityData entities,
        CancellationToken cancellationToken = default);
}

public interface IAuthorizationContext : IFeatureProvider
{
    ISelection Selection { get; }

    ITypeDefinition Type => Selection.Field.Type.AsTypeDefinition();

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
