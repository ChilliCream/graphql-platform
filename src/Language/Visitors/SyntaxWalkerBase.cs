namespace HotChocolate.Language
{
    public class SyntaxWalkerBase<TStart>
        : SyntaxVisitor<TStart>
        where TStart : ISyntaxNode
    {
        protected SyntaxWalkerBase()
        {
        }

        protected virtual void VisitUnsupportedDefinitions(
            IDefinitionNode node)
        {
        }

        protected override void VisitListValue(ListValueNode node)
        {
            VisitMany(node.Items, VisitValue);
        }

        protected override void VisitObjectValue(ObjectValueNode node)
        {
            VisitMany(node.Fields, VisitObjectField);
        }

        protected override void VisitObjectField(ObjectFieldNode node)
        {
            VisitName(node.Name);
            VisitValue(node.Value);
        }

        protected override void VisitVariable(VariableNode node)
        {
            VisitName(node.Name);
        }

        protected override void VisitDirective(DirectiveNode node)
        {
            VisitName(node.Name);
            VisitMany(node.Arguments, VisitArgument);
        }

        protected override void VisitArgument(ArgumentNode node)
        {
            VisitName(node.Name);
            VisitValue(node.Value);
        }

        protected override void VisitListType(ListTypeNode node)
        {
            VisitType(node.Type);
        }

        protected override void VisitNonNullType(NonNullTypeNode node)
        {
            VisitType(node.Type);
        }

        protected override void VisitNamedType(NamedTypeNode node)
        {
            VisitName(node.Name);
        }
    }
}
