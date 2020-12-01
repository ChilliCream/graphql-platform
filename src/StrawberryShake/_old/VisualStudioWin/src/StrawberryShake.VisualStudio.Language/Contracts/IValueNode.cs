using System;

namespace StrawberryShake.VisualStudio.Language
{
    public interface IValueNode
        : ISyntaxNode
        , IEquatable<IValueNode?>
    {
        object? Value { get; }
    }
}
