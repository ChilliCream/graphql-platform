using System;

namespace HotChocolate.Language
{
    public interface IValueNode
        : ISyntaxNode
        , IEquatable<IValueNode>
    {

    }

    public interface IValueNode<out T>
        : IValueNode
    {
        T Value { get; }
    }
}
