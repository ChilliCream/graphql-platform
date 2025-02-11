using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeUnionType(
    string name,
    string? description)
    : ICompositeNamedType
{
    private FrozenDictionary<string, CompositeObjectType> _types = default!;
    private DirectiveCollection _directives = default!;
    private bool _completed;

    public string Name { get; } = name;

    public string? Description { get; } = description;

    public DirectiveCollection Directives => _directives;

    public TypeKind Kind => TypeKind.Union;

    public ImmutableArray<CompositeObjectType> Types => _types.Values;

    public bool IsAssignableFrom(ICompositeNamedType type)
    {
        switch (type.Kind)
        {
            case TypeKind.Union:
                return ReferenceEquals(type, this);

            case TypeKind.Object:
                return _types.ContainsKey(((CompositeObjectType)type).Name);

            default:
                return false;
        }
    }

    internal void Complete(CompositeUnionTypeCompletionContext context)
    {
        if (_completed)
        {
            throw new NotSupportedException(
                "The type definition is sealed and cannot be modified.");
        }

        _directives = context.Directives;
        _types = context.Types;
        _completed = true;
    }

    public override string ToString() => Name;
}
