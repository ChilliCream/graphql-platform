using System;

namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxWalker
    {
        protected sealed override ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            ISyntaxVisitorContext context)
        {
            return node switch
            {
                DocumentNode n => Enter(n, context),
                OperationDefinitionNode n => Enter(n, context),
                VariableDefinitionNode n => Enter(n, context),
                VariableNode n => Enter(n, context),
                SelectionSetNode n => Enter(n, context),
                FieldNode n => Enter(n, context),
                ArgumentNode n => Enter(n, context),
                FragmentSpreadNode n => Enter(n, context),
                InlineFragmentNode n => Enter(n, context),
                FragmentDefinitionNode n => Enter(n, context),
                DirectiveNode n => Enter(n, context),
                NamedTypeNode n => Enter(n, context),
                ListTypeNode n => Enter(n, context),
                NonNullTypeNode n => Enter(n, context),
                ListValueNode n => Enter(n, context),
                ObjectValueNode n => Enter(n, context),
                ObjectFieldNode n => Enter(n, context),
                IValueNode n => Enter(n, context),
                SchemaDefinitionNode n => Enter(n, context),
                OperationTypeDefinitionNode n => Enter(n, context),
                ScalarTypeDefinitionNode n => Enter(n, context),
                ObjectTypeDefinitionNode n => Enter(n, context),
                FieldDefinitionNode n => Enter(n, context),
                InputValueDefinitionNode n => Enter(n, context),
                InterfaceTypeDefinitionNode n => Enter(n, context),
                UnionTypeDefinitionNode n => Enter(n, context),
                EnumTypeDefinitionNode n => Enter(n, context),
                EnumValueDefinitionNode n => Enter(n, context),
                InputObjectTypeDefinitionNode n => Enter(n, context),
                DirectiveDefinitionNode n => Enter(n, context),
                SchemaExtensionNode n => Enter(n, context),
                ScalarTypeExtensionNode n => Enter(n, context),
                ObjectTypeExtensionNode n => Enter(n, context),
                InterfaceTypeExtensionNode n => Enter(n, context),
                UnionTypeExtensionNode n => Enter(n, context),
                EnumTypeExtensionNode n => Enter(n, context),
                InputObjectTypeExtensionNode n => Enter(n, context),
                NameNode n => Enter(n, context),
                _ => throw new NotSupportedException(node.GetType().FullName)
            };
        }

        protected virtual ISyntaxVisitorAction Enter(
            DocumentNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            VariableNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            SelectionSetNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            FieldNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ArgumentNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            FragmentSpreadNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            FragmentDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            DirectiveNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            NamedTypeNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ListTypeNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            NonNullTypeNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ListValueNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            IValueNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            SchemaDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            OperationTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ScalarTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ObjectTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            FieldDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InputValueDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InterfaceTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            UnionTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            EnumTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            EnumValueDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InputObjectTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            DirectiveDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            SchemaExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ScalarTypeExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ObjectTypeExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InterfaceTypeExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            UnionTypeExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
           EnumTypeExtensionNode node,
           ISyntaxVisitorContext context) =>
           DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
           InputObjectTypeExtensionNode node,
           ISyntaxVisitorContext context) =>
           DefaultAction;
           
        protected virtual ISyntaxVisitorAction Enter(
            NameNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

    }
}
