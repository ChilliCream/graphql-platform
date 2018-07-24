using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class AllVariableUsagesAreAllowedVisitor
        : QueryVisitorErrorBase
    {
        private Dictionary<string, ITypeNode> _variables
            = new Dictionary<string, ITypeNode>();

        public AllVariableUsagesAreAllowedVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {

            foreach (VariableDefinitionNode variable in
                operation.VariableDefinitions)
            {
                _variables[variable.Variable.Name.Value] = variable.Type;
            }

            base.VisitOperationDefinition(operation, path);
        }

        protected override void VisitField(
           FieldNode field,
           IType type,
           ImmutableStack<ISyntaxNode> path)
        {
            ValidateArguments(field, field.Arguments);
            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            ValidateArguments(directive, directive.Arguments);
            base.VisitDirective(directive, path);
        }

        private void ValidateArguments(ISyntaxNode node, IEnumerable<ArgumentNode> arguments)
        {
            foreach (ArgumentNode argument in arguments)
            {

            }
        }
    }
}
