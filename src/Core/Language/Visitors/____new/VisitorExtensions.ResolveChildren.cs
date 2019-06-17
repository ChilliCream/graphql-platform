using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Language
{

    /*
    export const QueryDocumentKeys = {
      Name: [],

      Document: ['definitions'],
      OperationDefinition: [
        'name',
        'variableDefinitions',
        'directives',
        'selectionSet',
      ],
      VariableDefinition: ['variable', 'type', 'defaultValue', 'directives'],
      Variable: ['name'],
      SelectionSet: ['selections'],
      Field: ['alias', 'name', 'arguments', 'directives', 'selectionSet'],
      Argument: ['name', 'value'],

      FragmentSpread: ['name', 'directives'],
      InlineFragment: ['typeCondition', 'directives', 'selectionSet'],
      FragmentDefinition: [
        'name',
        // Note: fragment variable definitions are experimental and may be changed
        // or removed in the future.
        'variableDefinitions',
        'typeCondition',
        'directives',
        'selectionSet',
      ],

      IntValue: [],
      FloatValue: [],
      StringValue: [],
      BooleanValue: [],
      NullValue: [],
      EnumValue: [],
      ListValue: ['values'],
      ObjectValue: ['fields'],
      ObjectField: ['name', 'value'],

      Directive: ['name', 'arguments'],

      NamedType: ['name'],
      ListType: ['type'],
      NonNullType: ['type'],

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

    public static partial class VisitorExtensions
    {
        private static void ResolveChildren(
            ISyntaxNode node,
            IndexStack<SyntaxNodeInfo> children)
        {
            switch (node)
            {
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

        private static void ResolveChildren(
            ListValueNode node,
            IndexStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(nameof(node.Items), node.Items, children);
        }

        private static void ResolveChildren(
            ObjectValueNode node,
            IndexStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(nameof(node.Fields), node.Fields, children);
        }

        private static void ResolveChildren(
            ObjectFieldNode node,
            IndexStack<SyntaxNodeInfo> children)
        {
            ResolveChildren(nameof(node.Name), node.Name, children);
            ResolveChildren(nameof(node.Value), node.Value, children);
        }

        private static void ResolveChildren(
            string name,
            ISyntaxNode child,
            IndexStack<SyntaxNodeInfo> children)
        {
            if (child != null)
            {
                children.Push(new SyntaxNodeInfo(child, name));
            }
        }

        private static void ResolveChildren(
            string name,
            IReadOnlyList<ISyntaxNode> items,
            IndexStack<SyntaxNodeInfo> children)
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
