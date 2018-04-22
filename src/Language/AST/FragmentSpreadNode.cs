using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FragmentSpreadNode
        : ISelectionNode
    {
        public FragmentSpreadNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            Location = location;
            Name = name;
            Directives = directives;
        }

        public NodeKind Kind { get; } = NodeKind.FragmentSpread;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}