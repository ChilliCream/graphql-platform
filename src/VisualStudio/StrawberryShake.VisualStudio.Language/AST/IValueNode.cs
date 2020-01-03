using System;

namespace StrawberryShake.Language
{
    public interface IValueNode
        : ISyntaxNode
        , IEquatable<IValueNode?>
    {
        object? Value { get; }
    }
}
