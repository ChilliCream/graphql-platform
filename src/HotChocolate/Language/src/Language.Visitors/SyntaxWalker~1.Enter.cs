using System;

namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxWalker<TContext>
    {
        protected override ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            TContext context)
        {
            switch (node.Kind)
            {
                case SyntaxKind.Name:
                    return Enter((NameNode)node, context);
                case SyntaxKind.Document:
                    return Enter((DocumentNode)node, context);
                case SyntaxKind.OperationDefinition:
                    return Enter((OperationDefinitionNode)node, context);
                case SyntaxKind.VariableDefinition:
                    return Enter((VariableDefinitionNode)node, context);
                case SyntaxKind.Variable:
                    return Enter((VariableNode)node, context);
                case SyntaxKind.SelectionSet:
                    return Enter((SelectionSetNode)node, context);
                case SyntaxKind.Field:
                    return Enter((FieldNode)node, context);
                case SyntaxKind.Argument:
                    return Enter((ArgumentNode)node, context);
                case SyntaxKind.FragmentSpread:
                    return Enter((FragmentSpreadNode)node, context);
                case SyntaxKind.InlineFragment:
                    return Enter((InlineFragmentNode)node, context);
                case SyntaxKind.FragmentDefinition:
                    return Enter((FragmentDefinitionNode)node, context);
                case SyntaxKind.Directive:
                    return Enter((DirectiveNode)node, context);
                case SyntaxKind.NamedType:
                    return Enter((NamedTypeNode)node, context);
                case SyntaxKind.ListType:
                    return Enter((ListTypeNode)node, context);
                case SyntaxKind.NonNullType:
                    return Enter((NonNullTypeNode)node, context);
                case SyntaxKind.ListValue:
                    return Enter((ListValueNode)node, context);
                case SyntaxKind.ObjectValue:
                    return Enter((ObjectValueNode)node, context);
                case SyntaxKind.ObjectField:
                    return Enter((ObjectFieldNode)node, context);
                case SyntaxKind.BooleanValue:
                case SyntaxKind.EnumValue:
                case SyntaxKind.FloatValue:
                case SyntaxKind.IntValue:
                case SyntaxKind.NullValue:
                case SyntaxKind.StringValue:
                    return Enter((IValueNode)node, context);
                case SyntaxKind.SchemaDefinition:
                    return Enter((SchemaDefinitionNode)node, context);
                case SyntaxKind.OperationTypeDefinition:
                    return Enter((OperationTypeDefinitionNode)node, context);
                case SyntaxKind.ScalarTypeDefinition:
                    return Enter((ScalarTypeDefinitionNode)node, context);
                case SyntaxKind.ObjectTypeDefinition:
                    return Enter((ObjectTypeDefinitionNode)node, context);
                case SyntaxKind.FieldDefinition:
                    return Enter((FieldDefinitionNode)node, context);
                case SyntaxKind.InputValueDefinition:
                    return Enter((InputValueDefinitionNode)node, context);
                case SyntaxKind.InterfaceTypeDefinition:
                    return Enter((InterfaceTypeDefinitionNode)node, context);
                case SyntaxKind.UnionTypeDefinition:
                    return Enter((UnionTypeDefinitionNode)node, context);
                case SyntaxKind.EnumTypeDefinition:
                    return Enter((EnumTypeDefinitionNode)node, context);
                case SyntaxKind.EnumValueDefinition:
                    return Enter((EnumValueDefinitionNode)node, context);
                case SyntaxKind.InputObjectTypeDefinition:
                    return Enter((InputObjectTypeDefinitionNode)node, context);
                case SyntaxKind.DirectiveDefinition:
                    return Enter((DirectiveDefinitionNode)node, context);
                case SyntaxKind.SchemaExtension:
                    return Enter((SchemaExtensionNode)node, context);
                case SyntaxKind.ScalarTypeExtension:
                    return Enter((ScalarTypeExtensionNode)node, context);
                case SyntaxKind.ObjectTypeExtension:
                    return Enter((ObjectTypeExtensionNode)node, context);
                case SyntaxKind.InterfaceTypeExtension:
                    return Enter((InterfaceTypeExtensionNode)node, context);
                case SyntaxKind.UnionTypeExtension:
                    return Enter((UnionTypeExtensionNode)node, context);
                case SyntaxKind.EnumTypeExtension:
                    return Enter((EnumTypeExtensionNode)node, context);
                case SyntaxKind.InputObjectTypeExtension:
                    return Enter((InputObjectTypeExtensionNode)node, context);
                default:
                    throw new NotSupportedException(node.GetType().FullName);
            }
        }

        protected virtual ISyntaxVisitorAction Enter(
            NameNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            DocumentNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            VariableNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            SelectionSetNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            FieldNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ArgumentNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            FragmentSpreadNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            FragmentDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            DirectiveNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            NamedTypeNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ListTypeNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            NonNullTypeNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ListValueNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            IValueNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            SchemaDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            OperationTypeDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ScalarTypeDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ObjectTypeDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            FieldDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InputValueDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InterfaceTypeDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            UnionTypeDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            EnumTypeDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            EnumValueDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InputObjectTypeDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            DirectiveDefinitionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            SchemaExtensionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ScalarTypeExtensionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            ObjectTypeExtensionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            InterfaceTypeExtensionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
            UnionTypeExtensionNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
           EnumTypeExtensionNode node,
           TContext context) =>
           DefaultAction;

        protected virtual ISyntaxVisitorAction Enter(
           InputObjectTypeExtensionNode node,
           TContext context) =>
           DefaultAction;
    }
}
