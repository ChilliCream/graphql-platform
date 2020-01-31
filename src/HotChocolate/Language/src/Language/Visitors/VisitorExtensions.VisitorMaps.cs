using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public static partial class VisitorExtensions
    {
        private static readonly Dictionary<Type, IntVisitorFn> _enterVisitors =
            CreateEnterVisitors();

        private static readonly Dictionary<Type, IntVisitorFn> _leaveVisitors =
            CreateLeaveVisitors();

        private static Dictionary<Type, IntVisitorFn> CreateEnterVisitors()
        {
            var dict = new Dictionary<Type, IntVisitorFn>();

            AddEnterVisitor<DocumentNode>(dict);
            AddEnterVisitor<OperationDefinitionNode>(dict);
            AddEnterVisitor<VariableDefinitionNode>(dict);
            AddEnterVisitor<VariableNode>(dict);
            AddEnterVisitor<SelectionSetNode>(dict);
            AddEnterVisitor<FieldNode>(dict);
            AddEnterVisitor<ArgumentNode>(dict);
            AddEnterVisitor<FragmentSpreadNode>(dict);
            AddEnterVisitor<InlineFragmentNode>(dict);
            AddEnterVisitor<FragmentDefinitionNode>(dict);
            AddEnterVisitor<DirectiveNode>(dict);
            AddEnterVisitor<NamedTypeNode>(dict);
            AddEnterVisitor<ListTypeNode>(dict);
            AddEnterVisitor<NonNullTypeNode>(dict);
            AddEnterVisitor<ListValueNode>(dict);
            AddEnterVisitor<ObjectValueNode>(dict);
            AddEnterVisitor<ObjectFieldNode>(dict);
            AddEnterVisitor<SchemaDefinitionNode>(dict);
            AddEnterVisitor<OperationTypeDefinitionNode>(dict);
            AddEnterVisitor<ScalarTypeDefinitionNode>(dict);
            AddEnterVisitor<ObjectTypeDefinitionNode>(dict);
            AddEnterVisitor<FieldDefinitionNode>(dict);
            AddEnterVisitor<InputValueDefinitionNode>(dict);
            AddEnterVisitor<InterfaceTypeDefinitionNode>(dict);
            AddEnterVisitor<UnionTypeDefinitionNode>(dict);
            AddEnterVisitor<EnumTypeDefinitionNode>(dict);
            AddEnterVisitor<EnumValueDefinitionNode>(dict);
            AddEnterVisitor<InputObjectTypeDefinitionNode>(dict);
            AddEnterVisitor<DirectiveDefinitionNode>(dict);
            AddEnterVisitor<SchemaExtensionNode>(dict);
            AddEnterVisitor<ScalarTypeExtensionNode>(dict);
            AddEnterVisitor<ObjectTypeExtensionNode>(dict);
            AddEnterVisitor<InterfaceTypeExtensionNode>(dict);
            AddEnterVisitor<UnionTypeExtensionNode>(dict);
            AddEnterVisitor<EnumTypeExtensionNode>(dict);
            AddEnterVisitor<InputObjectTypeExtensionNode>(dict);
            AddEnterVisitor<NameNode>(dict);
            AddEnterVisitor<StringValueNode>(dict);
            AddEnterVisitor<IntValueNode>(dict);
            AddEnterVisitor<FloatValueNode>(dict);
            AddEnterVisitor<BooleanValueNode>(dict);
            AddEnterVisitor<EnumValueNode>(dict);

            return dict;
        }

        private static Dictionary<Type, IntVisitorFn> CreateLeaveVisitors()
        {
            var dict = new Dictionary<Type, IntVisitorFn>();

            AddLeaveVisitor<DocumentNode>(dict);
            AddLeaveVisitor<OperationDefinitionNode>(dict);
            AddLeaveVisitor<VariableDefinitionNode>(dict);
            AddLeaveVisitor<VariableNode>(dict);
            AddLeaveVisitor<SelectionSetNode>(dict);
            AddLeaveVisitor<FieldNode>(dict);
            AddLeaveVisitor<ArgumentNode>(dict);
            AddLeaveVisitor<FragmentSpreadNode>(dict);
            AddLeaveVisitor<InlineFragmentNode>(dict);
            AddLeaveVisitor<FragmentDefinitionNode>(dict);
            AddLeaveVisitor<DirectiveNode>(dict);
            AddLeaveVisitor<NamedTypeNode>(dict);
            AddLeaveVisitor<ListTypeNode>(dict);
            AddLeaveVisitor<NonNullTypeNode>(dict);
            AddLeaveVisitor<ListValueNode>(dict);
            AddLeaveVisitor<ObjectValueNode>(dict);
            AddLeaveVisitor<ObjectFieldNode>(dict);
            AddLeaveVisitor<SchemaDefinitionNode>(dict);
            AddLeaveVisitor<OperationTypeDefinitionNode>(dict);
            AddLeaveVisitor<ScalarTypeDefinitionNode>(dict);
            AddLeaveVisitor<ObjectTypeDefinitionNode>(dict);
            AddLeaveVisitor<FieldDefinitionNode>(dict);
            AddLeaveVisitor<InputValueDefinitionNode>(dict);
            AddLeaveVisitor<InterfaceTypeDefinitionNode>(dict);
            AddLeaveVisitor<UnionTypeDefinitionNode>(dict);
            AddLeaveVisitor<EnumTypeDefinitionNode>(dict);
            AddLeaveVisitor<EnumValueDefinitionNode>(dict);
            AddLeaveVisitor<InputObjectTypeDefinitionNode>(dict);
            AddLeaveVisitor<DirectiveDefinitionNode>(dict);
            AddLeaveVisitor<SchemaExtensionNode>(dict);
            AddLeaveVisitor<ScalarTypeExtensionNode>(dict);
            AddLeaveVisitor<ObjectTypeExtensionNode>(dict);
            AddLeaveVisitor<InterfaceTypeExtensionNode>(dict);
            AddLeaveVisitor<UnionTypeExtensionNode>(dict);
            AddLeaveVisitor<EnumTypeExtensionNode>(dict);
            AddLeaveVisitor<InputObjectTypeExtensionNode>(dict);
            AddLeaveVisitor<NameNode>(dict);
            AddLeaveVisitor<StringValueNode>(dict);
            AddLeaveVisitor<IntValueNode>(dict);
            AddLeaveVisitor<FloatValueNode>(dict);
            AddLeaveVisitor<BooleanValueNode>(dict);
            AddLeaveVisitor<EnumValueNode>(dict);

            return dict;
        }

        private static void AddEnterVisitor<T>(
            IDictionary<Type, IntVisitorFn> dict)
            where T : ISyntaxNode
        {
            dict.Add(typeof(T), CreateVisitor<T>(true));
        }

        private static void AddLeaveVisitor<T>(
           IDictionary<Type, IntVisitorFn> dict)
           where T : ISyntaxNode
        {
            dict.Add(typeof(T), CreateVisitor<T>(false));
        }

        private static IntVisitorFn CreateVisitor<T>(bool enter)
            where T : ISyntaxNode
        {
            if (enter)
            {
                return (visitor, node, parent, path, ancestors) =>
                {
                    if (visitor is ISyntaxNodeVisitor<T> typedVisitor)
                    {
                        return typedVisitor.Enter(
                            (T)node, parent, path, ancestors);
                    }
                    return VisitorAction.Default;
                };
            }
            else
            {
                return (visitor, node, parent, path, ancestors) =>
                {
                    if (visitor is ISyntaxNodeVisitor<T> typedVisitor)
                    {
                        return typedVisitor.Leave(
                            (T)node, parent, path, ancestors);
                    }
                    return VisitorAction.Default;
                };
            }
        }
    }
}
