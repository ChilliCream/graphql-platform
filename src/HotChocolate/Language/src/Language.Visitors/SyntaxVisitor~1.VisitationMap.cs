using System;
using System.Collections.Generic;

namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxVisitor<TContext>
    {
        private ISyntaxVisitorAction VisitChildren(
            ISyntaxNode node,
            TContext context)
        {
            switch (node.Kind)
            {
                case NodeKind.Field:
                    return VisitChildren((FieldNode)node, context);
                case NodeKind.Argument:
                    return VisitChildren((ArgumentNode)node, context);
                case NodeKind.FragmentSpread:
                    return VisitChildren((FragmentSpreadNode)node, context);
                case NodeKind.InlineFragment:
                    return VisitChildren((InlineFragmentNode)node, context);
                case NodeKind.FragmentDefinition:
                    return VisitChildren((FragmentDefinitionNode)node, context);
                case NodeKind.Document:
                    return VisitChildren((DocumentNode)node, context);
                case NodeKind.OperationDefinition:
                    return VisitChildren((OperationDefinitionNode)node, context);
                case NodeKind.VariableDefinition:
                    return VisitChildren((VariableDefinitionNode)node, context);
                case NodeKind.Variable:
                    return VisitChildren((VariableNode)node, context);
                case NodeKind.SelectionSet:
                    return VisitChildren((SelectionSetNode)node, context);
                case NodeKind.Directive:
                    return VisitChildren((DirectiveNode)node, context);
                case NodeKind.NamedType:
                    return VisitChildren((NamedTypeNode)node, context);
                case NodeKind.ListType:
                    return VisitChildren((ListTypeNode)node, context);
                case NodeKind.NonNullType:
                    return VisitChildren((NonNullTypeNode)node, context);
                case NodeKind.ListValue:
                    return VisitChildren((ListValueNode)node, context);
                case NodeKind.ObjectValue:
                    return VisitChildren((ObjectValueNode)node, context);
                case NodeKind.ObjectField:
                    return VisitChildren((ObjectFieldNode)node, context);
                case NodeKind.SchemaDefinition:
                    return VisitChildren((SchemaDefinitionNode)node, context);
                case NodeKind.OperationTypeDefinition:
                    return VisitChildren((OperationTypeDefinitionNode)node, context);
                case NodeKind.ScalarTypeDefinition:
                    return VisitChildren((ScalarTypeDefinitionNode)node, context);
                case NodeKind.ObjectTypeDefinition:
                    return VisitChildren((ObjectTypeDefinitionNode)node, context);
                case NodeKind.FieldDefinition:
                    return VisitChildren((FieldDefinitionNode)node, context);
                case NodeKind.InputValueDefinition:
                    return VisitChildren((InputValueDefinitionNode)node, context);
                case NodeKind.InterfaceTypeDefinition:
                    return VisitChildren((InterfaceTypeDefinitionNode)node, context);
                case NodeKind.UnionTypeDefinition:
                    return VisitChildren((UnionTypeDefinitionNode)node, context);
                case NodeKind.EnumTypeDefinition:
                    return VisitChildren((EnumTypeDefinitionNode)node, context);
                case NodeKind.EnumValueDefinition:
                    return VisitChildren((EnumValueDefinitionNode)node, context);
                case NodeKind.InputObjectTypeDefinition:
                    return VisitChildren((InputObjectTypeDefinitionNode)node, context);
                case NodeKind.DirectiveDefinition:
                    return VisitChildren((DirectiveDefinitionNode)node, context);
                case NodeKind.SchemaExtension:
                    return VisitChildren((SchemaExtensionNode)node, context);
                case NodeKind.ScalarTypeExtension:
                    return VisitChildren((ScalarTypeExtensionNode)node, context);
                case NodeKind.ObjectTypeExtension:
                    return VisitChildren((ObjectTypeExtensionNode)node, context);
                case NodeKind.InterfaceTypeExtension:
                    return VisitChildren((InterfaceTypeExtensionNode)node, context);
                case NodeKind.UnionTypeExtension:
                    return VisitChildren((UnionTypeExtensionNode)node, context);
                case NodeKind.EnumTypeExtension:
                    return VisitChildren((EnumTypeExtensionNode)node, context);
                case NodeKind.InputObjectTypeExtension:
                    return VisitChildren((InputObjectTypeExtensionNode)node, context);

                case NodeKind.Name:
                case NodeKind.StringValue:
                case NodeKind.IntValue:
                case NodeKind.FloatValue:
                case NodeKind.EnumValue:
                case NodeKind.BooleanValue:
                    return DefaultAction;

                default:
                    throw new NotSupportedException(node.GetType().FullName);
            }
        }

        private ISyntaxVisitorAction VisitChildren(
            DocumentNode node,
            TContext context)
        {
            for (int i = 0; i < node.Definitions.Count; i++)
            {
                var result = Visit(node.Definitions[i], context);
                if (result.Kind == SyntaxVisitorActionKind.Break)
                {
                    return result;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            OperationDefinitionNode node,
            TContext context)
        {
            if (node.Name is { })
            {
                if (Visit(node.Name, node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.VariableDefinitions.Count; i++)
            {
                if (Visit(node.VariableDefinitions[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.SelectionSet, node, context).IsBreak())
            {
                return Break;
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            VariableDefinitionNode node,
            TContext context)
        {
            if (Visit(node.Variable, node, context).IsBreak())
            {
                return Break;
            }

            if (Visit(node.Type, node, context).IsBreak())
            {
                return Break;
            }


            if (node.DefaultValue is { })
            {
                if (Visit(node.DefaultValue, node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            VariableNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }
            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            SelectionSetNode node,
            TContext context)
        {
            for (int i = 0; i < node.Selections.Count; i++)
            {
                if (Visit(node.Selections[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            FieldNode node,
            TContext context)
        {
            if (node.Alias is { })
            {
                if (Visit(node.Alias, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (Visit(node.Arguments[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (node.SelectionSet is { })
            {
                if (Visit(node.SelectionSet, node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ArgumentNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            if (Visit(node.Value, node, context).IsBreak())
            {
                return Break;
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            FragmentSpreadNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            InlineFragmentNode node,
            TContext context)
        {
            if (node.TypeCondition is { })
            {
                if (Visit(node.TypeCondition, node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.SelectionSet, node, context).IsBreak())
            {
                return Break;
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            FragmentDefinitionNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            if (node.TypeCondition is { })
            {
                if (Visit(node.TypeCondition, node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.SelectionSet, node, context).IsBreak())
            {
                return Break;
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            DirectiveNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (Visit(node.Arguments[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            NamedTypeNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }
            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ListTypeNode node,
            TContext context)
        {
            if (Visit(node.Type, node, context).IsBreak())
            {
                return Break;
            }
            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            NonNullTypeNode node,
            TContext context)
        {
            if (Visit(node.Type, node, context).IsBreak())
            {
                return Break;
            }
            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ListValueNode node,
            TContext context)
        {
            for (int i = 0; i < node.Items.Count; i++)
            {
                if (Visit(node.Items[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ObjectValueNode node,
            TContext context)
        {
            for (int i = 0; i < node.Fields.Count; i++)
            {
                if (Visit(node.Fields[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ObjectFieldNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            if (Visit(node.Value, node, context).IsBreak())
            {
                return Break;
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            SchemaDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.OperationTypes.Count; i++)
            {
                if (Visit(node.OperationTypes[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            OperationTypeDefinitionNode node,
            TContext context)
        {
            if (Visit(node.Type, node, context).IsBreak())
            {
                return Break;
            }
            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ScalarTypeDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ObjectTypeDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Interfaces.Count; i++)
            {
                if (Visit(node.Interfaces[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Fields.Count; i++)
            {
                if (Visit(node.Fields[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            FieldDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (Visit(node.Arguments[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            InputValueDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            if (node.DefaultValue is { })
            {
                if (Visit(node.DefaultValue, node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            InterfaceTypeDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Interfaces.Count; i++)
            {
                if (Visit(node.Interfaces[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Fields.Count; i++)
            {
                if (Visit(node.Fields[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            UnionTypeDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Types.Count; i++)
            {
                if (Visit(node.Types[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            EnumTypeDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Values.Count; i++)
            {
                if (Visit(node.Values[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            EnumValueDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            InputObjectTypeDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Fields.Count; i++)
            {
                if (Visit(node.Fields[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            DirectiveDefinitionNode node,
            TContext context)
        {
            if (node.Description is { })
            {
                if (Visit(node.Description, node, context).IsBreak())
                {
                    return Break;
                }
            }

            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (Visit(node.Arguments[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Locations.Count; i++)
            {
                if (Visit(node.Locations[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            SchemaExtensionNode node,
            TContext context)
        {
            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.OperationTypes.Count; i++)
            {
                if (Visit(node.OperationTypes[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ScalarTypeExtensionNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            ObjectTypeExtensionNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Interfaces.Count; i++)
            {
                if (Visit(node.Interfaces[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Fields.Count; i++)
            {
                if (Visit(node.Fields[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            InterfaceTypeExtensionNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Interfaces.Count; i++)
            {
                if (Visit(node.Interfaces[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Fields.Count; i++)
            {
                if (Visit(node.Fields[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            UnionTypeExtensionNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Types.Count; i++)
            {
                if (Visit(node.Types[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            EnumTypeExtensionNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Values.Count; i++)
            {
                if (Visit(node.Values[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        private ISyntaxVisitorAction VisitChildren(
            InputObjectTypeExtensionNode node,
            TContext context)
        {
            if (Visit(node.Name, node, context).IsBreak())
            {
                return Break;
            }

            for (int i = 0; i < node.Directives.Count; i++)
            {
                if (Visit(node.Directives[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            for (int i = 0; i < node.Fields.Count; i++)
            {
                if (Visit(node.Fields[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }
    }
}
