namespace HotChocolate.Language
{
    public class SyntaxWalkerBase<TStart, TContext>
        : SyntaxVisitor<TStart, TContext>
        where TStart : ISyntaxNode
    {
        protected SyntaxWalkerBase()
        {
        }

        protected virtual void VisitUnsupportedDefinitions(
            IDefinitionNode node,
            TContext context)
        {
        }

        protected override void VisitListValue(
            ListValueNode node,
            TContext context)
        {
            VisitMany(node.Items, context, VisitValue);
        }

        protected override void VisitObjectValue(
            ObjectValueNode node,
            TContext context)
        {
            VisitMany(node.Fields, context, VisitObjectField);
        }

        protected override void VisitObjectField(
            ObjectFieldNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitValue(node.Value, context);
        }

        protected override void VisitVariable(
            VariableNode node,
            TContext context)
        {
            VisitName(node.Name, context);
        }

        protected override void VisitDirective(
            DirectiveNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitMany(node.Arguments, context, VisitArgument);
        }

        protected override void VisitArgument(
            ArgumentNode node,
            TContext context)
        {
            VisitName(node.Name, context);
            VisitValue(node.Value, context);
        }

        protected override void VisitListType(
            ListTypeNode node,
            TContext context)
        {
            VisitType(node.Type, context);
        }

        protected override void VisitNonNullType(
            NonNullTypeNode node,
            TContext context)
        {
            VisitType(node.Type, context);
        }

        protected override void VisitNamedType(
            NamedTypeNode node,
            TContext context)
        {
            VisitName(node.Name, context);
        }
    }
}
