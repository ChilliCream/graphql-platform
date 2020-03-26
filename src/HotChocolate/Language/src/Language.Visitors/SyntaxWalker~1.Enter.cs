using System;

namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxWalker<TContext>
    {
        protected sealed override ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            TContext context)
        {
            switch (node.Kind)
            {
                case NodeKind.Name:
                    return Enter((NameNode)node, context);
                case NodeKind.Document:
                    return Enter((DocumentNode)node, context);
                case NodeKind.OperationDefinition:
                    return Enter((OperationDefinitionNode)node, context);
                case NodeKind.VariableDefinition:
                    return Enter((VariableDefinitionNode)node, context);
                case NodeKind.Variable:
                    return Enter((VariableNode)node, context);
                case NodeKind.SelectionSet:
                    return Enter((SelectionSetNode)node, context);
                case NodeKind.Field:
                    return Enter((FieldNode)node, context);
                case NodeKind.Argument:
                    return Enter((ArgumentNode)node, context);
                case NodeKind.FragmentSpread:
                    return Enter((FragmentSpreadNode)node, context);
                case NodeKind.InlineFragment:
                    return Enter((InlineFragmentNode)node, context);
                case NodeKind.FragmentDefinition:
                    return Enter((FragmentDefinitionNode)node, context);
                case NodeKind.Directive:
                    return Enter((DirectiveNode)node, context);
                case NodeKind.NamedType:
                    return Enter((NamedTypeNode)node, context);
                case NodeKind.ListType:
                    return Enter((ListTypeNode)node, context);
                case NodeKind.NonNullType:
                    return Enter((NonNullTypeNode)node, context);
                case NodeKind.ListValue:
                    return Enter((ListValueNode)node, context);
                case NodeKind.ObjectValue:
                    return Enter((ObjectValueNode)node, context);
                case NodeKind.ObjectField:
                    return Enter((ObjectFieldNode)node, context);
                case NodeKind.BooleanValue:
                case NodeKind.EnumValue:
                case NodeKind.FloatValue:
                case NodeKind.IntValue:
                case NodeKind.NullValue:
                case NodeKind.StringValue:
                    return Enter((IValueNode)node, context);
                case NodeKind.SchemaDefinition:
                    return Enter((SchemaDefinitionNode)node, context);
                case NodeKind.OperationTypeDefinition:
                    return Enter((OperationTypeDefinitionNode)node, context);
                case NodeKind.ScalarTypeDefinition:
                    return Enter((ScalarTypeDefinitionNode)node, context);
                case NodeKind.ObjectTypeDefinition:
                    return Enter((ObjectTypeDefinitionNode)node, context);
                case NodeKind.FieldDefinition:
                    return Enter((FieldDefinitionNode)node, context);
                case NodeKind.InputValueDefinition:
                    return Enter((InputValueDefinitionNode)node, context);
                case NodeKind.InterfaceTypeDefinition:
                    return Enter((InterfaceTypeDefinitionNode)node, context);
                case NodeKind.UnionTypeDefinition:
                    return Enter((UnionTypeDefinitionNode)node, context);
                case NodeKind.EnumTypeDefinition:
                    return Enter((EnumTypeDefinitionNode)node, context);
                case NodeKind.EnumValueDefinition:
                    return Enter((EnumValueDefinitionNode)node, context);
                case NodeKind.InputObjectTypeDefinition:
                    return Enter((InputObjectTypeDefinitionNode)node, context);
                case NodeKind.DirectiveDefinition:
                    return Enter((DirectiveDefinitionNode)node, context);
                case NodeKind.SchemaExtension:
                    return Enter((SchemaExtensionNode)node, context);
                case NodeKind.ScalarTypeExtension:
                    return Enter((ScalarTypeExtensionNode)node, context);
                case NodeKind.ObjectTypeExtension:
                    return Enter((ObjectTypeExtensionNode)node, context);
                case NodeKind.InterfaceTypeExtension:
                    return Enter((InterfaceTypeExtensionNode)node, context);
                case NodeKind.UnionTypeExtension:
                    return Enter((UnionTypeExtensionNode)node, context);
                case NodeKind.EnumTypeExtension:
                    return Enter((EnumTypeExtensionNode)node, context);
                case NodeKind.InputObjectTypeExtension:
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
