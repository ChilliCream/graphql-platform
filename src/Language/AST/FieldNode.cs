using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FieldNode
        : ISelectionNode
    {
        public FieldNode(
            Location location,
            NameNode name,
            NameNode alias,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<ArgumentNode> arguments,
            SelectionSetNode selectionSet)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            Location = location;
            Name = name;
            Alias = alias;
            Directives = directives;
            Arguments = arguments;
            SelectionSet = selectionSet;
        }

        public NodeKind Kind { get; } = NodeKind.Field;
        public Location Location { get; }
        public NameNode Name { get; }
        public NameNode Alias { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<ArgumentNode> Arguments { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}