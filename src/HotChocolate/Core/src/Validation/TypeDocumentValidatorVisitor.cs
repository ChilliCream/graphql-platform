using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Validation
{
    public class TypeDocumentValidatorVisitor : DocumentValidatorVisitor
    {
        internal static ObjectField TypeNameField { get; } =
            new(IntrospectionFields.CreateTypeNameField(DescriptorContext.Create()), default);

        protected TypeDocumentValidatorVisitor(SyntaxVisitorOptions options = default)
            : base(options)
        {
        }

        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            if (context.Schema.GetOperationType(node.Operation) is { } type)
            {
                context.Types.Push(type);
                context.Variables.Clear();
                return Continue;
            }

            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Variables[node.Variable.Name.Value] = node;
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            IDocumentValidatorContext context)
        {
            if (node.TypeCondition is null)
            {
                return Continue;
            }

            if (context.Schema.TryGetType(
                node.TypeCondition.Name.Value,
                out INamedOutputType type))
            {
                context.Types.Push(type);
                return Continue;
            }

            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            FragmentDefinitionNode node,
            IDocumentValidatorContext context)
        {
            if (context.Schema.TryGetType(
                node.TypeCondition.Name.Value,
                out INamedOutputType namedOutputType))
            {
                context.Types.Push(namedOutputType);
                return Continue;
            }

            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        protected override ISyntaxVisitorAction Leave(
           OperationDefinitionNode node,
           IDocumentValidatorContext context)
        {
            context.Types.Pop();
            context.Variables.Clear();
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            InlineFragmentNode node,
            IDocumentValidatorContext context)
        {
            if (node.TypeCondition is { })
            {
                context.Types.Pop();
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            FragmentDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Types.Pop();
            return Continue;
        }
    }
}
