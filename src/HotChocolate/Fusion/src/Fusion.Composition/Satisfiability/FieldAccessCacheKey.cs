using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Collections;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

internal readonly struct FieldAccessCacheKey(
    FieldNode selection,
    MutableObjectTypeDefinition type,
    SatisfiabilityPathItem? parent) : IEquatable<FieldAccessCacheKey>
{
    private readonly FieldNode _selection = selection;
    private readonly MutableObjectTypeDefinition _type = type;
    private readonly SatisfiabilityPathItem? _parent = parent;

    public bool Equals(FieldAccessCacheKey other)
        => ReferenceEquals(_selection, other._selection)
            && ReferenceEquals(_type, other._type)
            && ReferenceEquals(_parent, other._parent);

    public override bool Equals(object? obj) => obj is FieldAccessCacheKey other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(RuntimeHelpers.GetHashCode(_selection), _type, RuntimeHelpers.GetHashCode(_parent));
}
