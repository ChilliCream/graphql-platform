using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Language.Rewriters;

public class SyntaxRewriter<TContext>
    : ISyntaxRewriter<TContext>
    where TContext : ISyntaxVisitorContext
{
    public ISyntaxNode Rewrite(ISyntaxNode node, TContext context)
    {
        switch (node)
        {
            case ArgumentNode casted:
                return RewriteArgument(casted, context);

            case BooleanValueNode casted:
                return RewriteBooleanValue(casted, context);

            case DirectiveDefinitionNode casted:
                return RewriteDirectiveDefinition(casted, context);

            case DirectiveNode casted:
                return RewriteDirective(casted, context);

            case DocumentNode casted:
                return RewriteDocument(casted, context);

            case EnumTypeDefinitionNode casted:
                return RewriteEnumTypeDefinition(casted, context);

            case EnumTypeExtensionNode casted:
                return RewriteEnumTypeExtension(casted, context);

            case EnumValueDefinitionNode casted:
                return RewriteEnumValueDefinition(casted, context);

            case EnumValueNode casted:
                return RewriteEnumValue(casted, context);

            case FieldDefinitionNode casted:
                return RewriteFieldDefinition(casted, context);

            case FieldNode casted:
                return RewriteField(casted, context);

            case FloatValueNode casted:
                return RewriteFloatValue(casted, context);

            case FragmentDefinitionNode casted:
                return RewriteFragmentDefinition(casted, context);

            case FragmentSpreadNode casted:
                return RewriteFragmentSpread(casted, context);

            case InlineFragmentNode casted:
                return RewriteInlineFragment(casted, context);

            case InputObjectTypeDefinitionNode casted:
                return RewriteInputObjectTypeDefinition(casted, context);

            case InputObjectTypeExtensionNode casted:
                return RewriteInputObjectTypeExtension(casted, context);

            case InputValueDefinitionNode casted:
                break;

            case InterfaceTypeDefinitionNode casted:
                return RewriteInterfaceTypeDefinition(casted, context);

            case InterfaceTypeExtensionNode casted:
                return RewriteInterfaceTypeExtension(casted, context);

            case IntValueNode intValueNode:
                break;

            case ListNullabilityNode listNullabilityNode:
                break;

            case ListTypeNode listTypeNode:
                break;

            case ListValueNode listValueNode:
                break;

            case NamedTypeNode namedTypeNode:
                break;

            case NameNode nameNode:
                break;

            case NonNullTypeNode nonNullTypeNode:
                break;

            case NullValueNode nullValueNode:
                break;

            case ObjectFieldNode objectFieldNode:
                break;

            case ObjectTypeDefinitionNode casted:
                return RewriteObjectTypeDefinition(casted, context);

            case ObjectTypeExtensionNode casted:
                return RewriteObjectTypeExtension(casted, context);

            case ObjectValueNode objectValueNode:
                break;

            case OperationDefinitionNode operationDefinitionNode:
                break;

            case OperationTypeDefinitionNode operationTypeDefinitionNode:
                break;

            case OptionalModifierNode optionalModifierNode:
                break;

            case RequiredModifierNode requiredModifierNode:
                break;

            case ScalarTypeDefinitionNode scalarTypeDefinitionNode:
                break;

            case ScalarTypeExtensionNode scalarTypeExtensionNode:
                break;

            case SchemaCoordinateNode schemaCoordinateNode:
                break;

            case SchemaDefinitionNode schemaDefinitionNode:
                break;

            case SchemaExtensionNode schemaExtensionNode:
                break;

            case SelectionSetNode selectionSetNode:
                break;

            case StringValueNode stringValueNode:
                break;

            case UnionTypeDefinitionNode unionTypeDefinitionNode:
                break;

            case UnionTypeExtensionNode unionTypeExtensionNode:
                break;

            case VariableDefinitionNode variableDefinitionNode:
                break;

            case VariableNode variableNode:
                break;

            case ComplexTypeDefinitionNodeBase complexTypeDefinitionNodeBase:
                break;

            case EnumTypeDefinitionNodeBase enumTypeDefinitionNodeBase:
                break;

            case InputObjectTypeDefinitionNodeBase inputObjectTypeDefinitionNodeBase:
                break;

            case UnionTypeDefinitionNodeBase unionTypeDefinitionNodeBase:
                break;

            case NamedSyntaxNode namedSyntaxNode:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(node));
        }
    }

    protected virtual ArgumentNode RewriteArgument(
        ArgumentNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        IValueNode value = RewriteNode(node.Value, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(value, node.Value))
        {
            return new ArgumentNode(node.Location, name, value);
        }

        return node;
    }

    protected virtual BooleanValueNode RewriteBooleanValue(
        BooleanValueNode node,
        TContext context)
        => node;

    protected virtual DirectiveDefinitionNode RewriteDirectiveDefinition(
        DirectiveDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        StringValueNode? description = RewriteNode(node.Description, context);
        IReadOnlyList<InputValueDefinitionNode> arguments = RewriteList(node.Arguments, context);
        IReadOnlyList<NameNode> locations = RewriteList(node.Locations, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(arguments, node.Arguments) ||
            !ReferenceEquals(locations, node.Locations))
        {
            return new DirectiveDefinitionNode(
                node.Location,
                name,
                description,
                node.IsRepeatable,
                arguments,
                locations);
        }

        return node;
    }

    protected virtual DirectiveNode RewriteDirective(
        DirectiveNode node,
        TContext context)
        => node;

    protected virtual DocumentNode RewriteDocument(
        DocumentNode node,
        TContext context)
    {
        IReadOnlyList<IDefinitionNode> definitions = RewriteList(node.Definitions, context);

        if (!ReferenceEquals(definitions, node.Definitions))
        {
            return new DocumentNode(
                node.Location,
                definitions);
        }

        return node;
    }

    protected virtual EnumTypeDefinitionNode RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        StringValueNode? description = RewriteNode(node.Description, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<EnumValueDefinitionNode> values = RewriteList(node.Values, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(values, node.Values))
        {
            return new EnumTypeDefinitionNode(
                node.Location,
                name,
                description,
                directives,
                values);
        }

        return node;
    }


    protected virtual EnumTypeExtensionNode RewriteEnumTypeExtension(
        EnumTypeExtensionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<EnumValueDefinitionNode> values = RewriteList(node.Values, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(values, node.Values))
        {
            return new EnumTypeExtensionNode(
                node.Location,
                name,
                directives,
                values);
        }

        return node;
    }

    protected virtual EnumValueDefinitionNode RewriteEnumValueDefinition(
        EnumValueDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        StringValueNode? description = RewriteNode(node.Description, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(directives, node.Directives))
        {
            return new EnumValueDefinitionNode(
                node.Location,
                name,
                description,
                directives);
        }

        return node;
    }

    protected virtual EnumValueNode RewriteEnumValue(
        EnumValueNode node,
        TContext context)
        => node;

    protected virtual FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        StringValueNode? description = RewriteNode(node.Description, context);
        IReadOnlyList<InputValueDefinitionNode> arguments = RewriteList(node.Arguments, context);
        ITypeNode type = RewriteNode(node.Type, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(arguments, node.Arguments) ||
            !ReferenceEquals(type, node.Type) ||
            !ReferenceEquals(directives, node.Directives))
        {
            return new FieldDefinitionNode(
                node.Location,
                name,
                description,
                arguments,
                type,
                directives);
        }

        return node;
    }

    protected virtual FieldNode RewriteField(
        FieldNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        NameNode? alias = RewriteNode(node.Alias, context);
        INullabilityNode? required = RewriteNode(node.Required, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<ArgumentNode> arguments = RewriteList(node.Arguments, context);
        SelectionSetNode? selectionSet = RewriteNode(node.SelectionSet, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(alias, node.Alias) ||
            !ReferenceEquals(required, node.Required) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(arguments, node.Arguments) ||
            !ReferenceEquals(selectionSet, node.SelectionSet))
        {
            return new FieldNode(
                node.Location,
                name,
                alias,
                required,
                directives,
                arguments,
                selectionSet);
        }

        return node;
    }

    protected virtual FloatValueNode RewriteFloatValue(
        FloatValueNode node,
        TContext context)
        => node;

    protected virtual FragmentDefinitionNode RewriteFragmentDefinition(
        FragmentDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        IReadOnlyList<VariableDefinitionNode> variableDefinitions =
            RewriteList(node.VariableDefinitions, context);
        NamedTypeNode typeCondition = RewriteNode(node.TypeCondition, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        SelectionSetNode? selectionSet = RewriteNode(node.SelectionSet, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(variableDefinitions, node.VariableDefinitions) ||
            !ReferenceEquals(typeCondition, node.TypeCondition) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(selectionSet, node.SelectionSet))
        {
            return new FragmentDefinitionNode(
                node.Location,
                name,
                variableDefinitions,
                typeCondition,
                directives,
                selectionSet);
        }

        return node;
    }

    protected virtual FragmentSpreadNode RewriteFragmentSpread(
        FragmentSpreadNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(directives, node.Directives))
        {
            return new FragmentSpreadNode(
                node.Location,
                name,
                directives);
        }

        return node;
    }

    protected virtual InlineFragmentNode RewriteInlineFragment(
        InlineFragmentNode node,
        TContext context)
    {
        NamedTypeNode? typeCondition = RewriteNode(node.TypeCondition, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        SelectionSetNode? selectionSet = RewriteNode(node.SelectionSet, context);

        if (!ReferenceEquals(typeCondition, node.TypeCondition) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(selectionSet, node.SelectionSet))
        {
            return new InlineFragmentNode(
                node.Location,
                typeCondition,
                directives,
                selectionSet);
        }

        return node;
    }

    protected virtual InputObjectTypeDefinitionNode RewriteInputObjectTypeDefinition(
        InputObjectTypeDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        StringValueNode? description = RewriteNode(node.Description, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<InputValueDefinitionNode> fields = RewriteList(node.Fields, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(fields, node.Fields))
        {
            return new InputObjectTypeDefinitionNode(
                node.Location,
                name,
                description,
                directives,
                fields);
        }

        return node;
    }

    protected virtual InputObjectTypeExtensionNode RewriteInputObjectTypeExtension(
        InputObjectTypeExtensionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<InputValueDefinitionNode> fields = RewriteList(node.Fields, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(fields, node.Fields))
        {
            return new InputObjectTypeExtensionNode(
                node.Location,
                name,
                directives,
                fields);
        }

        return node;
    }

    protected virtual NameNode RewriteName(
        NameNode node,
        TContext context)
        => node;

    protected virtual VariableNode RewriteVariable(
        VariableNode node,
        TContext context)
    {
        VariableNode current = node;

        current = Rewrite(current, node.Name, context,
            RewriteName, current.WithName);

        return current;
    }



    protected virtual IntValueNode RewriteIntValue(
        IntValueNode node,
        TContext context)
    {
        return node;
    }

    protected virtual StringValueNode RewriteStringValue(
        StringValueNode node,
        TContext context)
    {
        return node;
    }





    protected virtual NullValueNode RewriteNullValue(
        NullValueNode node,
        TContext context)
    {
        return node;
    }

    protected virtual ListValueNode RewriteListValue(
        ListValueNode node,
        TContext context)
    {
        ListValueNode current = node;

        current = RewriteMany(current, current.Items, context,
            RewriteValue, current.WithItems);

        return current;
    }

    protected virtual ObjectValueNode RewriteObjectValue(
        ObjectValueNode node,
        TContext context)
    {
        ObjectValueNode current = node;

        current = RewriteMany(current, current.Fields, context,
            RewriteObjectField, current.WithFields);

        return current;
    }

    protected virtual ObjectFieldNode RewriteObjectField(
        ObjectFieldNode node,
        TContext context)
    {
        ObjectFieldNode current = node;

        current = Rewrite(current, node.Name, context,
            RewriteName, current.WithName);

        current = Rewrite(current, node.Value, context,
            RewriteValue, current.WithValue);

        return current;
    }



    protected virtual TParent RewriteDirectives<TParent>(
        TParent parent,
        IReadOnlyList<DirectiveNode> directives,
        TContext context,
        Func<IReadOnlyList<DirectiveNode>, TParent> rewrite)
    {
        return RewriteMany(parent, directives, context,
            RewriteDirective, rewrite);
    }

    protected virtual NamedTypeNode RewriteNamedType(
        NamedTypeNode node,
        TContext context)
    {
        NamedTypeNode current = node;

        current = Rewrite(current, node.Name, context,
            RewriteName, current.WithName);

        return current;
    }

    protected virtual ListTypeNode RewriteListType(
        ListTypeNode node,
        TContext context)
    {
        ListTypeNode current = node;

        current = Rewrite(current, current.Type, context,
            RewriteType, current.WithType);

        return current;
    }

    protected virtual NonNullTypeNode RewriteNonNullType(
        NonNullTypeNode node,
        TContext context)
    {
        NonNullTypeNode current = node;

        current = Rewrite(current, current.Type, context,
            (t, c) => (INullableTypeNode)RewriteType(t, c),
            current.WithType);

        return current;
    }

    protected virtual InterfaceTypeDefinitionNode RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        StringValueNode? description = RewriteNode(node.Description, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<NamedTypeNode> interfaces = RewriteList(node.Interfaces, context);
        IReadOnlyList<FieldDefinitionNode> fields = RewriteList(node.Fields, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(interfaces, node.Interfaces) ||
            !ReferenceEquals(fields, node.Fields))
        {
            return new InterfaceTypeDefinitionNode(
                node.Location,
                name,
                description,
                directives,
                interfaces,
                fields);
        }

        return node;
    }

    protected virtual InterfaceTypeExtensionNode RewriteInterfaceTypeExtension(
        InterfaceTypeExtensionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<NamedTypeNode> interfaces = RewriteList(node.Interfaces, context);
        IReadOnlyList<FieldDefinitionNode> fields = RewriteList(node.Fields, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(interfaces, node.Interfaces) ||
            !ReferenceEquals(fields, node.Fields))
        {
            return new InterfaceTypeExtensionNode(
                node.Location,
                name,
                directives,
                interfaces,
                fields);
        }

        return node;
    }

    protected virtual ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        StringValueNode? description = RewriteNode(node.Description, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<NamedTypeNode> interfaces = RewriteList(node.Interfaces, context);
        IReadOnlyList<FieldDefinitionNode> fields = RewriteList(node.Fields, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(interfaces, node.Interfaces) ||
            !ReferenceEquals(fields, node.Fields))
        {
            return new ObjectTypeDefinitionNode(
                node.Location,
                name,
                description,
                directives,
                interfaces,
                fields);
        }

        return node;
    }

    protected virtual ObjectTypeExtensionNode RewriteObjectTypeExtension(
        ObjectTypeExtensionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        IReadOnlyList<NamedTypeNode> interfaces = RewriteList(node.Interfaces, context);
        IReadOnlyList<FieldDefinitionNode> fields = RewriteList(node.Fields, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(interfaces, node.Interfaces) ||
            !ReferenceEquals(fields, node.Fields))
        {
            return new ObjectTypeExtensionNode(
                node.Location,
                name,
                directives,
                interfaces,
                fields);
        }

        return node;
    }

    private T RewriteNode<T>(T? node, TContext context) where T : ISyntaxNode
        => node is null ? default : (T)Rewrite(node, context);

    private IReadOnlyList<T> RewriteList<T>(IReadOnlyList<T> nodes, TContext context)
    {

    }
}

public interface ISyntaxRewriter<in TContext> where TContext : ISyntaxVisitorContext
{
    ISyntaxNode Rewrite(ISyntaxNode node, TContext context);
}
