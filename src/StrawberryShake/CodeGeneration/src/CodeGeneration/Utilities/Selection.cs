using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class Selection
    {
        public Selection(
            INamedType type,
            SelectionSetNode selectionSet,
            IReadOnlyList<FieldSelection> fields,
            IReadOnlyList<IFragmentNode> fragments)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            SelectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
            ExpandedSelectionSet = selectionSet.WithSelections(
                fields.Select(t => t.FieldSyntax).ToList());
        }

        public INamedType Type { get; }

        public SelectionSetNode SelectionSet { get; }

        public SelectionSetNode ExpandedSelectionSet { get; }

        public IReadOnlyList<FieldSelection> Fields { get; }

        public IReadOnlyList<IFragmentNode> Fragments { get; }
    }
}
