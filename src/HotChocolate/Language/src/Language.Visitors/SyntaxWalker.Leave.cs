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
                case NodeKind.Name:
                    return Leave((NameNode)node, context);
                case NodeKind.Document:
                    return Leave((DocumentNode)node, context);
                case NodeKind.OperationDefinition:
                    return Leave((OperationDefinitionNode)node, context);
                case NodeKind.VariableDefinition:
                    return Leave((VariableDefinitionNode)node, context);
                case NodeKind.Variable:
                    return Leave((VariableNode)node, context);
                case NodeKind.SelectionSet:
                    return Leave((SelectionSetNode)node, context);
                case NodeKind.Field:
                    return Leave((FieldNode)node, context);
                case NodeKind.Argument:
                    return Leave((ArgumentNode)node, context);
                case NodeKind.FragmentSpread:
                    return Leave((FragmentSpreadNode)node, context);
                case NodeKind.InlineFragment:
                    return Leave((InlineFragmentNode)node, context);
                case NodeKind.FragmentDefinition:
                    return Leave((FragmentDefinitionNode)node, context);
                case NodeKind.Directive:
                    return Leave((DirectiveNode)node, context);
                case NodeKind.NamedType:
                    return Leave((NamedTypeNode)node, context);
                case NodeKind.ListType:
                    return Leave((ListTypeNode)node, context);
                case NodeKind.NonNullType:
                    return Leave((NonNullTypeNode)node, context);
                case NodeKind.ListValue:
                    return Leave((ListValueNode)node, context);
                case NodeKind.ObjectValue:
                    return Leave((ObjectValueNode)node, context);
                case NodeKind.ObjectField:
                    return Leave((ObjectFieldNode)node, context);
                case NodeKind.BooleanValue:
                case NodeKind.EnumValue:
                case NodeKind.FloatValue:
                case NodeKind.IntValue:
                case NodeKind.NullValue:
                case NodeKind.StringValue:
                    return Leave((IValueNode)node, context);
                case NodeKind.SchemaDefinition:
                    return Leave((SchemaDefinitionNode)node, context);
                case NodeKind.OperationTypeDefinition:
                    return Leave((OperationTypeDefinitionNode)node, context);
                case NodeKind.ScalarTypeDefinition:
                    return Leave((ScalarTypeDefinitionNode)node, context);
                case NodeKind.ObjectTypeDefinition:
                    return Leave((ObjectTypeDefinitionNode)node, context);
                case NodeKind.FieldDefinition:
                    return Leave((FieldDefinitionNode)node, context);
                case NodeKind.InputValueDefinition:
                    return Leave((InputValueDefinitionNode)node, context);
                case NodeKind.InterfaceTypeDefinition:
                    return Leave((InterfaceTypeDefinitionNode)node, context);
                case NodeKind.UnionTypeDefinition:
                    return Leave((UnionTypeDefinitionNode)node, context);
                case NodeKind.EnumTypeDefinition:
                    return Leave((EnumTypeDefinitionNode)node, context);
                case NodeKind.EnumValueDefinition:
                    return Leave((EnumValueDefinitionNode)node, context);
                case NodeKind.InputObjectTypeDefinition:
                    return Leave((InputObjectTypeDefinitionNode)node, context);
                case NodeKind.InputUnionTypeDefinition:
                    return Leave((InputUnionTypeDefinitionNode)node, context);
                case NodeKind.DirectiveDefinition:
                    return Leave((DirectiveDefinitionNode)node, context);
                case NodeKind.SchemaExtension:
                    return Leave((SchemaExtensionNode)node, context);
                case NodeKind.ScalarTypeExtension:
                    return Leave((ScalarTypeExtensionNode)node, context);
                case NodeKind.ObjectTypeExtension:
                    return Leave((ObjectTypeExtensionNode)node, context);
                case NodeKind.InterfaceTypeExtension:
                    return Leave((InterfaceTypeExtensionNode)node, context);
                case NodeKind.UnionTypeExtension:
                    return Leave((UnionTypeExtensionNode)node, context);
                case NodeKind.EnumTypeExtension:
                    return Leave((EnumTypeExtensionNode)node, context);
                case NodeKind.InputObjectTypeExtension:
                    return Leave((InputObjectTypeExtensionNode)node, context);
                case NodeKind.InputUnionTypeExtension:
                    return Leave((InputUnionTypeExtensionNode)node, context);
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

        protected virtual ISyntaxVisitorAction Leave(
           InputUnionTypeExtensionNode node,
           ISyntaxVisitorContext context) =>
           DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
           InputUnionTypeDefinitionNode node,
           ISyntaxVisitorContext context) =>
           DefaultAction;
    }
}
