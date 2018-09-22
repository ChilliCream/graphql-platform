using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FragmentSpreadNode
        : NamedSyntaxNode
        , ISelectionNode
    {
        public FragmentSpreadNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives)
            : base(location, name, directives) { }

        public override NodeKind Kind { get; } = NodeKind.FragmentSpread;
    }
}
