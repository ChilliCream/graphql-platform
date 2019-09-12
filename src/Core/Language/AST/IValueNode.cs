using System;

namespace HotChocolate.Language
{
    public interface IValueNode
        : ISyntaxNode
        , IEquatable<IValueNode?>
    {
        object? Value { get; }

        Span<byte> AsSpan();
    }

    public interface IValueNode<out T>
        : IValueNode
    {
        new T Value { get; }
    }
}
