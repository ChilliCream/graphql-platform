using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class VisitationMap
        : IVisitationMap
    {
        public virtual void ResolveChildren(
            ISyntaxNode node,
            IList<SyntaxNodeInfo> children)
        {
            switch (node)
            {
                case DocumentNode document:
                    ResolveChildren(document, children);
                    break;

                case OperationDefinitionNode operationDefinition:
                    ResolveChildren(operationDefinition, children);
                    break;

                case VariableDefinitionNode variableDefinition:
                    ResolveChildren(variableDefinition, children);
                    break;

                case VariableNode variable:
                    ResolveChildren(variable, children);
                    break;

                case SelectionSetNode selectionSet:
                    ResolveChildren(selectionSet, children);
                    break;

                case FieldNode field:
                    ResolveChildren(field, children);
                    break;

                case ArgumentNode argument:
                    ResolveChildren(argument, children);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    ResolveChildren(fragmentSpread, children);
                    break;

                case InlineFragmentNode inlineFragment:
                    ResolveChildren(inlineFragment, children);
                    break;

                case FragmentDefinitionNode fragmentDefinition:
                    ResolveChildren(fragmentDefinition, children);
                    break;

                case DirectiveNode directive:
                    ResolveChildren(directive, children);
                    break;

                case NamedTypeNode namedType:
                    ResolveChildren(namedType, children);
                    break;

                case ListTypeNode listType:
                    ResolveChildren(listType, children);
                    break;

                case NonNullTypeNode nonNullType:
                    ResolveChildren(nonNullType, children);
                    break;

                case ListValueNode list:
                    ResolveChildren(list, children);
                    break;

                case ObjectValueNode objectValue:
                    ResolveChildren(objectValue, children);
                    break;

                case ObjectFieldNode objectField:
                    ResolveChildren(objectField, children);
                    break;

                case SchemaDefinitionNode schemaDefinition:
                    ResolveChildren(schemaDefinition, children);
                    break;

                case OperationTypeDefinitionNode operationTypeDefinition:
                    ResolveChildren(operationTypeDefinition, children);
                    break;

                case ScalarTypeDefinitionNode scalarTypeDefinition:
                    ResolveChildren(scalarTypeDefinition, children);
                    break;

                case ObjectTypeDefinitionNode objectTypeDefinition:
                    ResolveChildren(objectTypeDefinition, children);
                    break;

                case FieldDefinitionNode fieldDefinition:
                    ResolveChildren(fieldDefinition, children);
                    break;

                case InputValueDefinitionNode inputValueDefinition:
                    ResolveChildren(inputValueDefinition, children);
                    break;

                case InterfaceTypeDefinitionNode interfaceTypeDefinition:
                    ResolveChildren(interfaceTypeDefinition, children);
                    break;

                case UnionTypeDefinitionNode unionTypeDefinitionNode:
                    ResolveChildren(unionTypeDefinitionNode, children);
                    break;

                case EnumTypeDefinitionNode enumTypeDefinition:
                    ResolveChildren(enumTypeDefinition, children);
                    break;

                case EnumValueDefinitionNode enumValueDefinition:
                    ResolveChildren(enumValueDefinition, children);
                    break;

                case InputObjectTypeDefinitionNode inputObjectTypeDefinition:
                    ResolveChildren(inputObjectTypeDefinition, children);
                    break;

                case DirectiveDefinitionNode directiveDefinition:
                    ResolveChildren(directiveDefinition, children);
                    break;

                case SchemaExtensionNode schemaExtension:
                    ResolveChildren(schemaExtension, children);
                    break;

                case ScalarTypeExtensionNode scalarTypeExtension:
                    ResolveChildren(scalarTypeExtension, children);
                    break;

                case ObjectTypeExtensionNode objectTypeExtension:
                    ResolveChildren(objectTypeExtension, children);
                    break;

                case InterfaceTypeExtensionNode interfaceTypeExtension:
                    ResolveChildren(interfaceTypeExtension, children);
                    break;

                case UnionTypeExtensionNode unionTypeExtension:
                    ResolveChildren(unionTypeExtension, children);
                    break;

                case EnumTypeExtensionNode enumTypeExtension:
                    ResolveChildren(enumTypeExtension, children);
                    break;

                case InputObjectTypeExtensionNode inputObjectTypeExtension:
                    ResolveChildren(inputObjectTypeExtension, children);
                    break;
            }
        }

        protected virtual void ResolveChildren(
            DocumentNode node,
            IList<SyntaxNodeInfo> children)
        {
            if (node.Definitions.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Definitions),
                    node.Definitions,
                    children);
            }
        }

        protected virtual void ResolveChildren(
            OperationDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            if (node.Name != null)
            {
                ResolveChildren(
                    nameof(node.Name),
                    node.Name,
                    children);
            }

            if (node.VariableDefinitions.Count != 0)
            {
                ResolveChildren(
                    nameof(node.VariableDefinitions),
                    node.VariableDefinitions,
                    children);
            }

            if (node.Directives.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Directives),
                    node.Directives,
                    children);
            }

            ResolveChildren(
                nameof(node.SelectionSet),
                node.SelectionSet,
                children);
        }

        protected virtual void ResolveChildren(
            VariableDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Variable),
                node.Variable,
                children);

            ResolveChildren(
                nameof(node.Type),
                node.Type,
                children);

            if (node.DefaultValue != null)
            {
                ResolveChildren(
                    nameof(node.DefaultValue),
                    node.DefaultValue,
                    children);
            }

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);
        }

        protected virtual void ResolveChildren(
            VariableNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);
        }

        protected virtual void ResolveChildren(
            SelectionSetNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Selections),
                node.Selections,
                children);
        }

        protected virtual void ResolveChildren(
            FieldNode node,
            IList<SyntaxNodeInfo> children)
        {
            if (node.Alias != null)
            {
                ResolveChildren(
                    nameof(node.Alias),
                    node.Alias,
                    children);
            }

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            if (node.Arguments.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Arguments),
                    node.Arguments,
                    children);
            }

            if (node.Directives.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Directives),
                    node.Directives,
                    children);
            }

            if (node.SelectionSet != null)
            {
                ResolveChildren(
                    nameof(node.SelectionSet),
                    node.SelectionSet,
                    children);
            }
        }

        protected virtual void ResolveChildren(
            ArgumentNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Value),
                node.Value,
                children);
        }

        protected virtual void ResolveChildren(
            FragmentSpreadNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            if (node.Directives.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Directives),
                    node.Directives,
                    children);
            }
        }

        protected virtual void ResolveChildren(
            InlineFragmentNode node,
            IList<SyntaxNodeInfo> children)
        {
            if (node.TypeCondition != null)
            {
                ResolveChildren(
                    nameof(node.TypeCondition),
                    node.TypeCondition,
                    children);
            }

            if (node.Directives.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Directives),
                    node.Directives,
                    children);
            }

            ResolveChildren(
                nameof(node.SelectionSet),
                node.SelectionSet,
                children);
        }

        protected virtual void ResolveChildren(
            FragmentDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            if (node.VariableDefinitions.Count != 0)
            {
                ResolveChildren(
                    nameof(node.VariableDefinitions),
                    node.VariableDefinitions,
                    children);
            }

            ResolveChildren(
                nameof(node.TypeCondition),
                node.TypeCondition,
                children);

            if (node.Directives.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Directives),
                    node.Directives,
                    children);
            }

            ResolveChildren(
                nameof(node.SelectionSet),
                node.SelectionSet,
                children);
        }

        protected virtual void ResolveChildren(
            SchemaDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.OperationTypes),
                node.OperationTypes,
                children);
        }

        protected virtual void ResolveChildren(
            OperationTypeDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Type),
                node.Type,
                children);
        }

        protected virtual void ResolveChildren(
            ScalarTypeDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);
        }

        protected virtual void ResolveChildren(
            ObjectTypeDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Interfaces),
                node.Interfaces,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Fields),
                node.Fields,
                children);
        }

        protected virtual void ResolveChildren(
            FieldDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Arguments),
                node.Arguments,
                children);

            ResolveChildren(
                nameof(node.Type),
                node.Type,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);
        }

        protected virtual void ResolveChildren(
            InputValueDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Type),
                node.Type,
                children);

            ResolveChildren(
                nameof(node.DefaultValue),
                node.DefaultValue,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);
        }

        protected virtual void ResolveChildren(
            InterfaceTypeDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Fields),
                node.Fields,
                children);
        }

        protected virtual void ResolveChildren(
            UnionTypeDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Types),
                node.Types,
                children);
        }

        protected virtual void ResolveChildren(
            EnumTypeDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Values),
                node.Values,
                children);
        }

        protected virtual void ResolveChildren(
           EnumValueDefinitionNode node,
           IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);
        }

        protected virtual void ResolveChildren(
            InputObjectTypeDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Fields),
                node.Fields,
                children);
        }

        protected virtual void ResolveChildren(
            DirectiveDefinitionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Description),
                node.Description,
                children);

            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Arguments),
                node.Arguments,
                children);

            ResolveChildren(
                nameof(node.Locations),
                node.Locations,
                children);
        }

        protected virtual void ResolveChildren(
            SchemaExtensionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.OperationTypes),
                node.OperationTypes,
                children);
        }

        protected virtual void ResolveChildren(
            ScalarTypeExtensionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);
        }

        protected virtual void ResolveChildren(
            ObjectTypeExtensionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Interfaces),
                node.Interfaces,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Fields),
                node.Fields,
                children);
        }

        protected virtual void ResolveChildren(
            InterfaceTypeExtensionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Fields),
                node.Fields,
                children);
        }

        protected virtual void ResolveChildren(
            UnionTypeExtensionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Types),
                node.Types,
                children);
        }

        protected virtual void ResolveChildren(
            EnumTypeExtensionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Values),
                node.Values,
                children);
        }

        protected virtual void ResolveChildren(
            InputObjectTypeExtensionNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            ResolveChildren(
                nameof(node.Directives),
                node.Directives,
                children);

            ResolveChildren(
                nameof(node.Fields),
                node.Fields,
                children);
        }

        protected virtual void ResolveChildren(
            DirectiveNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            if (node.Arguments.Count != 0)
            {
                ResolveChildren(
                    nameof(node.Arguments),
                    node.Arguments,
                    children);
            }
        }

        protected virtual void ResolveChildren(
            NamedTypeNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);
        }

        protected virtual void ResolveChildren(
            ListTypeNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Type),
                node.Type,
                children);
        }

        protected virtual void ResolveChildren(
            NonNullTypeNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Type),
                node.Type,
                children);
        }

        protected virtual void ResolveChildren(
            ListValueNode node,
            IList<SyntaxNodeInfo> children)
        {
            if (node.Items.Count != 0)
            {
                ResolveChildren(nameof(node.Items), node.Items, children);
            }
        }

        protected virtual void ResolveChildren(
            ObjectValueNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(nameof(node.Fields), node.Fields, children);
        }

        protected virtual void ResolveChildren(
            ObjectFieldNode node,
            IList<SyntaxNodeInfo> children)
        {
            ResolveChildren(nameof(node.Name), node.Name, children);
            ResolveChildren(nameof(node.Value), node.Value, children);
        }

        protected void ResolveChildren(
            string name,
            ISyntaxNode child,
            IList<SyntaxNodeInfo> children)
        {
            if (child != null)
            {
                children.Push(new SyntaxNodeInfo(child, name));
            }
        }

        protected void ResolveChildren(
            string name,
            IReadOnlyList<ISyntaxNode> items,
            IList<SyntaxNodeInfo> children)
        {
            if (items.Count == 0)
            {
                return;
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                children.Push(new SyntaxNodeInfo(items[i], name, i));
            }
        }
    }
}
