using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class ValuesOfCorrectTypeVisitor
        : QueryVisitorErrorBase
    {
        public ValuesOfCorrectTypeVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {

        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<Language.ISyntaxNode> path)
        {
            IOutputField f;
            f.Arguments
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {

        }

        private void VisitArgument(
            ISyntaxNode node,
            IFieldCollection<IInputField> arguments,
            string argumentName,
            IValueNode argumentValue,
            ImmutableStack<ISyntaxNode> path)
        {
            if (arguments.TryGetField(argumentName, out IInputField argument))
            {
                if (argumentValue is VariableNode)
                {
                    if()
                }
                else if (!argument.Type.IsInstanceOfType(argumentValue))
                {
                    Errors.Add(new ValidationError("", node));
                }
            }
        }
    }
}
