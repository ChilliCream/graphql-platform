using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FieldNode
        : NamedSyntaxNode
        , ISelectionNode
    {
        public FieldNode(
            Location? location,
            NameNode name,
            NameNode? alias,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<ArgumentNode> arguments,
            SelectionSetNode? selectionSet)
            : base(location, name, directives)
        {
            Alias = alias;
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
            SelectionSet = selectionSet;
        }

        public override NodeKind Kind { get; } = NodeKind.Field;

        public NameNode? Alias { get; }

        public IReadOnlyList<ArgumentNode> Arguments { get; }

        public SelectionSetNode? SelectionSet { get; }


        public FieldNode WithLocation(Location? location)
        {
            return new FieldNode(location, Name, Alias,
                Directives, Arguments, SelectionSet);
        }

        public FieldNode WithName(NameNode name)
        {
            return new FieldNode(Location, name, Alias,
                Directives, Arguments, SelectionSet);
        }

        public FieldNode WithAlias(NameNode? alias)
        {
            return new FieldNode(Location, Name, alias,
                Directives, Arguments, SelectionSet);
        }

        public FieldNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new FieldNode(Location, Name, Alias,
                directives, Arguments, SelectionSet);
        }

        public FieldNode WithArguments(
            IReadOnlyList<ArgumentNode> arguments)
        {
            return new FieldNode(Location, Name, Alias,
                Directives, arguments, SelectionSet);
        }

        public FieldNode WithSelectionSet(SelectionSetNode? selectionSet)
        {
            return new FieldNode(Location, Name, Alias,
                Directives, Arguments, selectionSet);
        }
    }
}
