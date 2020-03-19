using System;

namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxWalker
    {
        protected sealed override ISyntaxVisitorAction Leave(
            ISyntaxNode node,
            ISyntaxVisitorContext context)
        {
            return node switch
            {
                DocumentNode n => Leave(n, context),
                OperationDefinitionNode n => Leave(n, context),
                VariableDefinitionNode n => Leave(n, context),
                VariableNode n => Leave(n, context),
                SelectionSetNode n => Leave(n, context),
                FieldNode n => Leave(n, context),
                ArgumentNode n => Leave(n, context),
                FragmentSpreadNode n => Leave(n, context),
                InlineFragmentNode n => Leave(n, context),
                FragmentDefinitionNode n => Leave(n, context),
                DirectiveNode n => Leave(n, context),
                NamedTypeNode n => Leave(n, context),
                ListTypeNode n => Leave(n, context),
                NonNullTypeNode n => Leave(n, context),
                ListValueNode n => Leave(n, context),
                ObjectValueNode n => Leave(n, context),
                ObjectFieldNode n => Leave(n, context),
                SchemaDefinitionNode n => Leave(n, context),
                OperationTypeDefinitionNode n => Leave(n, context),
                ScalarTypeDefinitionNode n => Leave(n, context),
                ObjectTypeDefinitionNode n => Leave(n, context),
                FieldDefinitionNode n => Leave(n, context),
                InputValueDefinitionNode n => Leave(n, context),
                InterfaceTypeDefinitionNode n => Leave(n, context),
                UnionTypeDefinitionNode n => Leave(n, context),
                EnumTypeDefinitionNode n => Leave(n, context),
                EnumValueDefinitionNode n => Leave(n, context),
                InputObjectTypeDefinitionNode n => Leave(n, context),
                DirectiveDefinitionNode n => Leave(n, context),
                SchemaExtensionNode n => Leave(n, context),
                ScalarTypeExtensionNode n => Leave(n, context),
                ObjectTypeExtensionNode n => Leave(n, context),
                InterfaceTypeExtensionNode n => Leave(n, context),
                UnionTypeExtensionNode n => Leave(n, context),
                EnumTypeExtensionNode n => Leave(n, context),
                InputObjectTypeExtensionNode n => Leave(n, context),
                _ => throw new NotSupportedException()
            };
        }

        protected virtual ISyntaxVisitorAction Leave(
            DocumentNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            OperationDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            VariableDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            VariableNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            SelectionSetNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            FieldNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ArgumentNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            FragmentSpreadNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            InlineFragmentNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            FragmentDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            DirectiveNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            NamedTypeNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ListTypeNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            NonNullTypeNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ListValueNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            SchemaDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            OperationTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ScalarTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ObjectTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            FieldDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            InputValueDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            InterfaceTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            UnionTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            EnumTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            EnumValueDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            InputObjectTypeDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            DirectiveDefinitionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            SchemaExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ScalarTypeExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ObjectTypeExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            InterfaceTypeExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            UnionTypeExtensionNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
           EnumTypeExtensionNode node,
           ISyntaxVisitorContext context) =>
           DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
           InputObjectTypeExtensionNode node,
           ISyntaxVisitorContext context) =>
           DefaultAction;
    }
}
