using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Delegation
{
    public class ExtractedField
    {
        public ExtractedField(
            FieldNode field,
            IReadOnlyList<VariableDefinitionNode> variables,
            IReadOnlyList<FragmentDefinitionNode> fragments)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Variables = variables ?? throw new ArgumentNullException(nameof(variables));
            Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public FieldNode Field { get; }

        public IReadOnlyList<VariableDefinitionNode> Variables { get; }

        public IReadOnlyList<FragmentDefinitionNode> Fragments { get; }
    }
}
