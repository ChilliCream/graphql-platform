using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Utilities
{
    internal sealed class FieldCollectionResult
    {
        public FieldCollectionResult(
            ObjectType type,
            IReadOnlyList<FieldSelection> fields,
            IReadOnlyList<IFragmentNode> fragments)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public ObjectType Type { get; }
        public IReadOnlyList<FieldSelection> Fields { get; }
        public IReadOnlyList<IFragmentNode> Fragments { get; }
    }
}
