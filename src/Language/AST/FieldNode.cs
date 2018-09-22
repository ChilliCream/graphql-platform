using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FieldNode
        : NamedSyntaxNode
    {
        public FieldNode(
            Location location,
            NameNode name,
            NameNode alias,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<ArgumentNode> arguments,
            SelectionSetNode selectionSet)
            : base(location, name, directives)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            Alias = alias;
            Arguments = arguments;
            SelectionSet = selectionSet;
        }

        public override NodeKind Kind { get; } = NodeKind.Field;

        public NameNode Alias { get; }

        public IReadOnlyCollection<ArgumentNode> Arguments { get; }

        public SelectionSetNode SelectionSet { get; }
    }
}
