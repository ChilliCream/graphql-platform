using System;

namespace HotChocolate.Language
{
    public interface IValueNode
        : ISyntaxNode
        , IEquatable<IValueNode?>
    {
        object? Value { get; }
    }
}
