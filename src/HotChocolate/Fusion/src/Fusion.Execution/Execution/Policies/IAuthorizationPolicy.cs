using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
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

public interface IAuthorizationContext
{
    ISelection Selection { get; }

    ITypeDefinition Type => Selection.Field.Type.AsTypeDefinition();
}

public readonly struct EntityData
{
    private readonly CompositeResultElement[] _entities;
    private readonly int _entitiesCount;

    public ITypeDefinition Type { get; }
    public ReadOnlySpan<CompositeResultElement> Entities
        => new(_entities, 0, _entitiesCount);
}
