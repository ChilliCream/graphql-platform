using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language;

public static class SyntaxComparer
{
    public static IEqualityComparer<ISyntaxNode> Default { get; }
        = new DefaultSyntaxEqualityComparer();

    public static IEqualityComparer<ISyntaxNode> ByReference { get; }
        = new ReferenceSyntaxEqualityComparer();

    private sealed class ReferenceSyntaxEqualityComparer : IEqualityComparer<ISyntaxNode>
    {
        public bool Equals(ISyntaxNode? x, ISyntaxNode? y)
            => ReferenceEquals(x, y);

        public int GetHashCode(ISyntaxNode obj)
            => RuntimeHelpers.GetHashCode(obj);
    }

    private sealed class DefaultSyntaxEqualityComparer : IEqualityComparer<ISyntaxNode>
    {
        public bool Equals(ISyntaxNode? x, ISyntaxNode? y)
            => object.Equals(x, y);

        public int GetHashCode(ISyntaxNode obj)
            => obj.GetHashCode();
    }
}
