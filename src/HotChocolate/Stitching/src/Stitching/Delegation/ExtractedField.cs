using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Delegation
{
    public class ExtractedField
    {
        public ExtractedField(
            IReadOnlyList<FieldNode> syntaxNodes,
            IReadOnlyList<VariableDefinitionNode> variables,
            IReadOnlyList<FragmentDefinitionNode> fragments)
        {
            SyntaxNodes = syntaxNodes ?? throw new ArgumentNullException(nameof(syntaxNodes));
            Variables = variables ?? throw new ArgumentNullException(nameof(variables));
            Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public IReadOnlyList<FieldNode> SyntaxNodes { get; }

        public IReadOnlyList<VariableDefinitionNode> Variables { get; }

        public IReadOnlyList<FragmentDefinitionNode> Fragments { get; }
    }
}
