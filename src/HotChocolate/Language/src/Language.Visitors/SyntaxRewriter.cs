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
                return RewriteInputValueDefinition(casted, context);

            case InterfaceTypeDefinitionNode casted:
                return RewriteInterfaceTypeDefinition(casted, context);

            case InterfaceTypeExtensionNode casted:
                return RewriteInterfaceTypeExtension(casted, context);

            case IntValueNode casted:
                return RewriteIntValue(casted, context);

            case ListNullabilityNode casted:
                return RewriteListNullability(casted, context);

            case ListTypeNode casted:
                return RewriteListType(casted, context);

            case ListValueNode casted:
                return RewriteListValue(casted, context);

            case NamedTypeNode casted:
                return RewriteNamedType(casted, context);

            case NameNode casted:
                return RewriteName(casted, context);

            case NonNullTypeNode casted:
                return RewriteNonNullType(casted, context);

            case NullValueNode casted:
                return RewriteNullValue(casted, context);

            case ObjectFieldNode casted:
                return RewriteObjectField(casted, context);

            case ObjectTypeDefinitionNode casted:
                return RewriteObjectTypeDefinition(casted, context);

            case ObjectTypeExtensionNode casted:
                return RewriteObjectTypeExtension(casted, context);

            case ObjectValueNode casted:
                return RewriteObjectValue(casted, context);

            case OperationDefinitionNode casted:
                return RewriteOperationDefinition(casted, context);

            case OperationTypeDefinitionNode casted:
                return RewriteOperationTypeDefinition(casted, context);

            case OptionalModifierNode casted:
                break;

            case RequiredModifierNode casted:
                break;

            case ScalarTypeDefinitionNode casted:
                break;

            case ScalarTypeExtensionNode casted:
                break;

            case SchemaCoordinateNode casted:
                break;

            case SchemaDefinitionNode casted:
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

    protected virtual InputValueDefinitionNode RewriteInputValueDefinition(
        InputValueDefinitionNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        StringValueNode? description = RewriteNode(node.Description, context);
        ITypeNode type = RewriteNode(node.Type, context);
        IValueNode? defaultValue = RewriteNode(node.DefaultValue, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(type, node.Type) ||
            !ReferenceEquals(defaultValue, node.DefaultValue) ||
            !ReferenceEquals(directives, node.Directives))
        {
            return new InputValueDefinitionNode(
                node.Location,
                name,
                description,
                type,
                defaultValue,
                directives);
        }

        return node;
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

    protected virtual IntValueNode RewriteIntValue(
        IntValueNode node,
        TContext context)
        => node;

    protected virtual ListNullabilityNode RewriteListNullability(
        ListNullabilityNode node,
        TContext context)
    {
        INullabilityNode? element = RewriteNode(node.Element, context);

        if (!ReferenceEquals(element, node.Element))
        {
            return new ListNullabilityNode(
                node.Location,
                element);
        }

        return node;
    }

    protected virtual ListTypeNode RewriteListType(
        ListTypeNode node,
        TContext context)
    {
        ITypeNode type = RewriteNode(node.Type, context);

        if (!ReferenceEquals(type, node.Type))
        {
            return new ListTypeNode(node.Location, type);
        }

        return node;
    }

    protected virtual ListValueNode RewriteListValue(
        ListValueNode node,
        TContext context)
    {
        IReadOnlyList<IValueNode> items = RewriteList(node.Items, context);

        if (!ReferenceEquals(items, node.Items))
        {
            return new ListValueNode(node.Location, items);
        }

        return node;
    }

    protected virtual NamedTypeNode RewriteNamedType(
        NamedTypeNode node,
        TContext context)
    {
        NameNode name = RewriteName(node.Name, context);

        if (!ReferenceEquals(name, node.Name))
        {
            return new NamedTypeNode(node.Location, name);
        }

        return node;
    }

    protected virtual NameNode RewriteName(
        NameNode node,
        TContext context)
        => node;

    protected virtual NonNullTypeNode RewriteNonNullType(
        NonNullTypeNode node,
        TContext context)
    {
        INullableTypeNode type = RewriteNode(node.Type, context);

        if (!ReferenceEquals(type, node.Type))
        {
            return new NonNullTypeNode(node.Location, type);
        }

        return node;
    }

    protected virtual NullValueNode RewriteNullValue(
        NullValueNode node,
        TContext context)
        => node;

    protected virtual ObjectFieldNode RewriteObjectField(
        ObjectFieldNode node,
        TContext context)
    {
        NameNode name = RewriteNode(node.Name, context);
        IValueNode value = RewriteNode(node.Value, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(value, node.Value))
        {
            return new ObjectFieldNode(node.Location, name, value);
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

    protected virtual ObjectValueNode RewriteObjectValue(
        ObjectValueNode node,
        TContext context)
    {
        IReadOnlyList<ObjectFieldNode> fields = RewriteList(node.Fields, context);

        if (!ReferenceEquals(fields, node.Fields))
        {
            return new ObjectValueNode(node.Location, fields);
        }

        return node;
    }

    protected virtual OperationDefinitionNode RewriteOperationDefinition(
        OperationDefinitionNode node,
        TContext context)
    {
        NameNode? name = RewriteNode(node.Name, context);
        IReadOnlyList<VariableDefinitionNode> variableDefinitions =
            RewriteList(node.VariableDefinitions, context);
        IReadOnlyList<DirectiveNode> directives = RewriteList(node.Directives, context);
        SelectionSetNode selectionSet = RewriteNode(node.SelectionSet, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(variableDefinitions, node.VariableDefinitions) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(selectionSet, node.SelectionSet))
        {
            return new OperationDefinitionNode(
                node.Location,
                name,
                node.Operation,
                variableDefinitions,
                directives,
                selectionSet);
        }

        return node;
    }

    protected virtual OperationTypeDefinitionNode RewriteOperationTypeDefinition(
        OperationTypeDefinitionNode node,
        TContext context)
    {
        NamedTypeNode type = RewriteNode(node.Type, context);

        if (!ReferenceEquals(type, node.Type))
        {
            return new OperationTypeDefinitionNode(
                node.Location,
                node.Operation,
                type);
        }

        return node;
    }


    protected virtual VariableNode RewriteVariable(
        VariableNode node,
        TContext context)
    {
        NameNode name = RewriteName(node.Name, context);

        if (!ReferenceEquals(name, node.Name))
        {
            return new VariableNode(node.Location, name);
        }

        return node;
    }





    protected virtual StringValueNode RewriteStringValue(
        StringValueNode node,
        TContext context)
    {
        return node;
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
