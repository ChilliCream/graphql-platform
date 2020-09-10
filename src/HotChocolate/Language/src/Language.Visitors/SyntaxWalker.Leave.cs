using System;

namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxWalker
    {
        protected override ISyntaxVisitorAction Leave(
            ISyntaxNode node,
            ISyntaxVisitorContext context)
        {
            switch (node.Kind)
            {
                case SyntaxKind.Name:
                    return Leave((NameNode)node, context);
                case SyntaxKind.Document:
                    return Leave((DocumentNode)node, context);
                case SyntaxKind.OperationDefinition:
                    return Leave((OperationDefinitionNode)node, context);
                case SyntaxKind.VariableDefinition:
                    return Leave((VariableDefinitionNode)node, context);
                case SyntaxKind.Variable:
                    return Leave((VariableNode)node, context);
                case SyntaxKind.SelectionSet:
                    return Leave((SelectionSetNode)node, context);
                case SyntaxKind.Field:
                    return Leave((FieldNode)node, context);
                case SyntaxKind.Argument:
                    return Leave((ArgumentNode)node, context);
                case SyntaxKind.FragmentSpread:
                    return Leave((FragmentSpreadNode)node, context);
                case SyntaxKind.InlineFragment:
                    return Leave((InlineFragmentNode)node, context);
                case SyntaxKind.FragmentDefinition:
                    return Leave((FragmentDefinitionNode)node, context);
                case SyntaxKind.Directive:
                    return Leave((DirectiveNode)node, context);
                case SyntaxKind.NamedType:
                    return Leave((NamedTypeNode)node, context);
                case SyntaxKind.ListType:
                    return Leave((ListTypeNode)node, context);
                case SyntaxKind.NonNullType:
                    return Leave((NonNullTypeNode)node, context);
                case SyntaxKind.ListValue:
                    return Leave((ListValueNode)node, context);
                case SyntaxKind.ObjectValue:
                    return Leave((ObjectValueNode)node, context);
                case SyntaxKind.ObjectField:
                    return Leave((ObjectFieldNode)node, context);
                case SyntaxKind.BooleanValue:
                case SyntaxKind.EnumValue:
                case SyntaxKind.FloatValue:
                case SyntaxKind.IntValue:
                case SyntaxKind.NullValue:
                case SyntaxKind.StringValue:
                    return Leave((IValueNode)node, context);
                case SyntaxKind.SchemaDefinition:
                    return Leave((SchemaDefinitionNode)node, context);
                case SyntaxKind.OperationTypeDefinition:
                    return Leave((OperationTypeDefinitionNode)node, context);
                case SyntaxKind.ScalarTypeDefinition:
                    return Leave((ScalarTypeDefinitionNode)node, context);
                case SyntaxKind.ObjectTypeDefinition:
                    return Leave((ObjectTypeDefinitionNode)node, context);
                case SyntaxKind.FieldDefinition:
                    return Leave((FieldDefinitionNode)node, context);
                case SyntaxKind.InputValueDefinition:
                    return Leave((InputValueDefinitionNode)node, context);
                case SyntaxKind.InterfaceTypeDefinition:
                    return Leave((InterfaceTypeDefinitionNode)node, context);
                case SyntaxKind.UnionTypeDefinition:
                    return Leave((UnionTypeDefinitionNode)node, context);
                case SyntaxKind.EnumTypeDefinition:
                    return Leave((EnumTypeDefinitionNode)node, context);
                case SyntaxKind.EnumValueDefinition:
                    return Leave((EnumValueDefinitionNode)node, context);
                case SyntaxKind.InputObjectTypeDefinition:
                    return Leave((InputObjectTypeDefinitionNode)node, context);
                case SyntaxKind.DirectiveDefinition:
                    return Leave((DirectiveDefinitionNode)node, context);
                case SyntaxKind.SchemaExtension:
                    return Leave((SchemaExtensionNode)node, context);
                case SyntaxKind.ScalarTypeExtension:
                    return Leave((ScalarTypeExtensionNode)node, context);
                case SyntaxKind.ObjectTypeExtension:
                    return Leave((ObjectTypeExtensionNode)node, context);
                case SyntaxKind.InterfaceTypeExtension:
                    return Leave((InterfaceTypeExtensionNode)node, context);
                case SyntaxKind.UnionTypeExtension:
                    return Leave((UnionTypeExtensionNode)node, context);
                case SyntaxKind.EnumTypeExtension:
                    return Leave((EnumTypeExtensionNode)node, context);
                case SyntaxKind.InputObjectTypeExtension:
                    return Leave((InputObjectTypeExtensionNode)node, context);
                default:
                    throw new NotSupportedException(node.GetType().FullName);
            }
        }
        protected virtual ISyntaxVisitorAction Leave(
            NameNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

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
            IValueNode node,
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
