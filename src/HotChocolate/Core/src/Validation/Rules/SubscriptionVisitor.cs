using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// Subscription operations must have exactly one root field.
    ///
    /// http://spec.graphql.org/June2018/#sec-Single-root-field
    /// </summary>
    public class SubscriptionVisitor : DocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            if (node.Operation == OperationType.Subscription)
            {
                return Continue;
            }

            return Skip;
        }

        protected override ISyntaxVisitorAction Leave(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            if (context.Names.Count > 1)
            {
                context.Errors.Add(context.SubscriptionSingleRootField(node));
            }
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Add((node.Alias ?? node.Name).Value);
            return Skip;
        }
    }
}
