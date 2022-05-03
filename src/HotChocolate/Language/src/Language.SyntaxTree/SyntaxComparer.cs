using System.Collections.Generic;

namespace HotChocolate.Language;

public static class SyntaxComparer
{
    public static IEqualityComparer<ISyntaxNode> BySyntax { get; }
        = new SyntaxEqualityComparer();

    public static IEqualityComparer<ISyntaxNode> ByReference { get; }
        = new DefaultSyntaxEqualityComparer();

    private sealed class DefaultSyntaxEqualityComparer : IEqualityComparer<ISyntaxNode>
    {
        public bool Equals(ISyntaxNode? x, ISyntaxNode? y)
            => object.Equals(x, y);

        public int GetHashCode(ISyntaxNode obj)
            => obj.GetHashCode();
    }
}
