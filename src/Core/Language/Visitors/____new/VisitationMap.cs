using System.Diagnostics.Tracing;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Language
{

    /*
    export const QueryDocumentKeys = {
      Name: [],

      SchemaDefinition: ['directives', 'operationTypes'],
      OperationTypeDefinition: ['type'],

      ScalarTypeDefinition: ['description', 'name', 'directives'],
      ObjectTypeDefinition: [
        'description',
        'name',
        'interfaces',
        'directives',
        'fields',
      ],
      FieldDefinition: ['description', 'name', 'arguments', 'type', 'directives'],
      InputValueDefinition: [
        'description',
        'name',
        'type',
        'defaultValue',
        'directives',
      ],
      InterfaceTypeDefinition: ['description', 'name', 'directives', 'fields'],
      UnionTypeDefinition: ['description', 'name', 'directives', 'types'],
      EnumTypeDefinition: ['description', 'name', 'directives', 'values'],
      EnumValueDefinition: ['description', 'name', 'directives'],
      InputObjectTypeDefinition: ['description', 'name', 'directives', 'fields'],

      DirectiveDefinition: ['description', 'name', 'arguments', 'locations'],

      SchemaExtension: ['directives', 'operationTypes'],

      ScalarTypeExtension: ['name', 'directives'],
      ObjectTypeExtension: ['name', 'interfaces', 'directives', 'fields'],
      InterfaceTypeExtension: ['name', 'directives', 'fields'],
      UnionTypeExtension: ['name', 'directives', 'types'],
      EnumTypeExtension: ['name', 'directives', 'values'],
      InputObjectTypeExtension: ['name', 'directives', 'fields'],
    };

     */

    public interface IVisitationMap
    {
        void ResolveChildren(ISyntaxNode node, IStack<SyntaxNodeInfo> children);
    }

    public class VisitationMap
        : IVisitationMap
    {

        public virtual void ResolveChildren(
            ISyntaxNode node,
            IStack<SyntaxNodeInfo> children)
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
            }
        }

        protected virtual void ResolveChildren(
            DocumentNode node,
            IStack<SyntaxNodeInfo> children)
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
            IStack<SyntaxNodeInfo> children)
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
            IStack<SyntaxNodeInfo> children)
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
            IStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);
        }

        protected virtual void ResolveChildren(
            SelectionSetNode node,
            IStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Selections),
                node.Selections,
                children);
        }

        protected virtual void ResolveChildren(
            FieldNode node,
            IStack<SyntaxNodeInfo> children)
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
            IStack<SyntaxNodeInfo> children)
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
            IStack<SyntaxNodeInfo> children)
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
            IStack<SyntaxNodeInfo> children)
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
            IStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);

            if (node.Directives.Count != 0)
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
            DirectiveNode node,
            IStack<SyntaxNodeInfo> children)
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
            IStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Name),
                node.Name,
                children);
        }

        protected virtual void ResolveChildren(
            ListTypeNode node,
            IStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Type),
                node.Type,
                children);
        }

        protected virtual void ResolveChildren(
            NonNullTypeNode node,
            IStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(
                nameof(node.Type),
                node.Type,
                children);
        }

        protected virtual void ResolveChildren(
            ListValueNode node,
            IStack<SyntaxNodeInfo> children)
        {
            if (node.Items.Count != 0)
            {
                ResolveChildren(nameof(node.Items), node.Items, children);
            }
        }

        protected virtual void ResolveChildren(
            ObjectValueNode node,
            IStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(nameof(node.Fields), node.Fields, children);
        }

        protected virtual void ResolveChildren(
            ObjectFieldNode node,
            IStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(nameof(node.Name), node.Name, children);
            ResolveChildren(nameof(node.Value), node.Value, children);
        }

        protected void ResolveChildren(
            string name,
            ISyntaxNode child,
            IStack<SyntaxNodeInfo> children)
        {
            if (child != null)
            {
                children.Push(new SyntaxNodeInfo(child, name));
            }
        }

        protected void ResolveChildren(
            string name,
            IReadOnlyList<ISyntaxNode> items,
            IStack<SyntaxNodeInfo> children)
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
