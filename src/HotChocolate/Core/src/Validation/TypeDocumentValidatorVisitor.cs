using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Validation
{
    public class TypeDocumentValidatorVisitor
       : DocumentValidatorVisitor
    {
        internal static __TypeNameField TypeNameField { get; } =
            new __TypeNameField(DescriptorContext.Create());

        protected TypeDocumentValidatorVisitor(SyntaxVisitorOptions options = default)
            : base(options)
        {
        }

        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            if (GetOperationType(context.Schema, node.Operation) is { } type)
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
            else if (context.Schema.TryGetType(
                node.TypeCondition.Name.Value,
                out INamedOutputType type))
            {
                context.Types.Push(type);
                return Continue;
            }
            else
            {
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
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
            else
            {
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
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

        private static ObjectType GetOperationType(
            ISchema schema,
            OperationType operation)
        {
            switch (operation)
            {
                case Language.OperationType.Query:
                    return schema.QueryType;
                case Language.OperationType.Mutation:
                    return schema.MutationType;
                case Language.OperationType.Subscription:
                    return schema.SubscriptionType;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
