using System;

namespace HotChocolate.Language
{
    public interface IValueNode
        : ISyntaxNode
        , IEquatable<IValueNode>
    {
        object Value { get; }
    }

    public interface IValueNode<out T>
        : IValueNode
    {
        new T Value { get; }
    }
}
