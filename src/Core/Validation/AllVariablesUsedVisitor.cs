using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class AllVariablesUsedVisitor
        : QueryVisitorErrorBase
    {
        private readonly HashSet<string> _usedVariables = new HashSet<string>();

        public AllVariablesUsedVisitor(ISchema schema)
            : base(schema)
        {
        }

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
                        ImmutableStack<ISyntaxNode>.Empty.Push(document));

                    declaredVariables.ExceptWith(_usedVariables);
                    if (declaredVariables.Count > 0)
                    {
                        Errors.Add(new ValidationError(
                            "The following variables were not used: " +
                            $"{string.Join(", ", declaredVariables)}.",
                            operation));
                    }

                    declaredVariables.Clear();
                    _usedVariables.Clear();
                }
            }
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            VisitArguments(field.Arguments);
            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            VisitArguments(directive.Arguments);
            base.VisitDirective(directive, path);
        }

        private void VisitArguments(IEnumerable<ArgumentNode> arguments)
        {
            foreach (ArgumentNode argumentNode in arguments)
            {
                if (argumentNode.Value is VariableNode v)
                {
                    _usedVariables.Add(v.Value);
                }
            }
        }
    }
}
