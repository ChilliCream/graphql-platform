using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        protected override void VisitDocument(
            DocumentNode document,
            ImmutableStack<ISyntaxNode> path)
        {
            var declaredVariables = new HashSet<string>();

            foreach (OperationDefinitionNode operation in document.Definitions
                .OfType<OperationDefinitionNode>())
            {
                foreach (var variableName in operation.VariableDefinitions
                    .Select(t => t.Variable.Name.Value))
                {
                    declaredVariables.Add(variableName);
                }

                VisitOperationDefinition(operation,
                    path.Push(document));

                var unusedVariables = new HashSet<string>(
                    declaredVariables);

                unusedVariables.ExceptWith(_usedVariables);
                if (unusedVariables.Count > 0)
                {
                    Errors.Add(new ValidationError(
                        "The following variables were not used: " +
                        $"{string.Join(", ", unusedVariables)}.",
                        operation));
                }

                _usedVariables.ExceptWith(declaredVariables);
                if (_usedVariables.Count > 0)
                {
                    Errors.Add(new ValidationError(
                        "The following variables were not declared: " +
                        $"{string.Join(", ", _usedVariables)}.",
                        operation));
                }

                declaredVariables.Clear();
                _usedVariables.Clear();
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
                VisitValue(argumentNode.Value);
            }
        }

        private void VisitValue(IValueNode value)
        {
            if (value is VariableNode v)
            {
                _usedVariables.Add(v.Value);
            }

            if (value is ObjectValueNode o)
            {
                foreach(ObjectFieldNode field in o.Fields)
                {
                    VisitValue(field.Value);
                }
            }

            if (value is ListValueNode l)
            {
                foreach (IValueNode item in l.Items)
                {
                    VisitValue(item);
                }
            }
        }
    }
}
