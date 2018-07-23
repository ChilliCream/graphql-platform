using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal class AllVariablesUsedVisitor
        : QueryVisitor
    {
        private readonly HashSet<string> _usedVariables = new HashSet<string>();
        private readonly List<ValidationError> _errors =
            new List<ValidationError>();

        public AllVariablesUsedVisitor(ISchema schema)
            : base(schema)
        {
        }

        public IReadOnlyCollection<ValidationError> Errors => _errors;

        public override void VisitDocument(DocumentNode document)
        {
            HashSet<string> declaredVariables = new HashSet<string>();

            foreach (OperationDefinitionNode operation in document.Definitions
                .OfType<OperationDefinitionNode>())
            {
                if (operation.VariableDefinitions.Count > 0)
                {
                    foreach (var variableName in operation.VariableDefinitions
                        .Select(t => t.Variable.Name.Value))
                    {
                        declaredVariables.Add(variableName);
                    }

                    VisitOperationDefinition(operation,
                        ImmutableStack<ISyntaxNode>.Empty);

                    declaredVariables.ExceptWith(_usedVariables);
                    if (declaredVariables.Count > 0)
                    {
                        _errors.Add(new ValidationError(
                            "The following variables were not used: " +
                            $"{string.Join(", ", declaredVariables)}.",
                            operation));
                    }

                    declaredVariables.Clear();
                    _usedVariables.Clear();
                }
            }
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode fragmentSpread,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (path.Last() is DocumentNode d)
            {
                string fragmentName = fragmentSpread.Name.Value;
                IEnumerable<FragmentDefinitionNode> fragments = d.Definitions
                    .OfType<FragmentDefinitionNode>()
                    .Where(t => t.Name.Value.EqualsOrdinal(fragmentName));

                foreach (FragmentDefinitionNode fragment in fragments)
                {
                    VisitFragmentDefinition(fragment,
                        path.Push(fragmentSpread));
                }
            }
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (ArgumentNode argumentNode in field.Arguments)
            {
                if (argumentNode.Value is VariableNode v)
                {
                    _usedVariables.Add(v.Value);
                }
            }

            base.VisitField(field, type, path);
        }
    }


}
