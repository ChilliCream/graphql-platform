using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Utilities
{
    internal sealed class FieldCollectionResult
    {
        public FieldCollectionResult(
            INamedType type,
            SelectionSetNode selectionSet,
            IReadOnlyList<FieldSelection> fields,
            IReadOnlyList<IFragmentNode> fragments)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            SelectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public INamedType Type { get; }

        public SelectionSetNode SelectionSet { get; }

        public IReadOnlyList<FieldSelection> Fields { get; }

        public IReadOnlyList<IFragmentNode> Fragments { get; }
    }
}
