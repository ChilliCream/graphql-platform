using System;

namespace HotChocolate.Language
{
    public readonly struct SyntaxNodeInfo
    {
        public SyntaxNodeInfo(ISyntaxNode node, string? name)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Name = name;
            Index = null;
        }

        public SyntaxNodeInfo(ISyntaxNode node, string? name, int? index)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Name = name;
            Index = index;
        }

        public ISyntaxNode Node { get; }

        public string? Name { get; }

        public int? Index { get; }
    }
}
