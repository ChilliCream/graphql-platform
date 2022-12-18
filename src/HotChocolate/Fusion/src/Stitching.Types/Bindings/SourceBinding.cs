
using System;

namespace HotChocolate.Stitching.Types.Bindings;

internal readonly struct SourceBinding : IBinding, IEquatable<SourceBinding>
{
    public SourceBinding(SchemaCoordinate target, string source)
    {
        Target = target;
        Source = source;
    }

    public SchemaCoordinate Target { get; }

    public string Source { get; }

    public bool Equals(SourceBinding other)
        => Target.Equals(other.Target) && Source == other.Source;

    public override bool Equals(object? obj)
        => obj is SourceBinding other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Target, Source);

    public static bool operator ==(SourceBinding left, SourceBinding right)
        => left.Equals(right);

    public static bool operator !=(SourceBinding left, SourceBinding right)
        => !left.Equals(right);
}
