using System;
using System.Buffers;
using System.Collections.Generic;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Language.Rewriters;

public partial class SyntaxRewriter<TContext>
    : ISyntaxRewriter<TContext>
    where TContext : ISyntaxVisitorContext
{
    protected SyntaxRewriter()
    {
    }

    public ISyntaxNode Rewrite(ISyntaxNode node, TContext context)
    {
        TContext nodeContext = OnBeforeRewrite(node, context);
        ISyntaxNode rewrittenNode = RewriteNode(node, nodeContext);
        OnAfterRewrite(node, nodeContext);
        return rewrittenNode;
    }

    protected virtual ISyntaxNode RewriteNode(ISyntaxNode node, TContext context)
        => node.Kind switch
        {
            SyntaxKind.Name => RewriteName((NameNode)node, context),
            SyntaxKind.Document => RewriteDocument((DocumentNode)node, context),
            SyntaxKind.OperationDefinition => RewriteOperationDefinition(
                (OperationDefinitionNode)node,
                context),
            SyntaxKind.VariableDefinition => RewriteVariableDefinition(
                (VariableDefinitionNode)node,
                context),
            SyntaxKind.Variable => RewriteVariable((VariableNode)node, context),
            SyntaxKind.SelectionSet => RewriteSelectionSet((SelectionSetNode)node, context),
            SyntaxKind.Field => RewriteField((FieldNode)node, context),
            SyntaxKind.Argument => RewriteArgument((ArgumentNode)node, context),
            SyntaxKind.FragmentSpread => RewriteFragmentSpread((FragmentSpreadNode)node, context),
            SyntaxKind.InlineFragment => RewriteInlineFragment((InlineFragmentNode)node, context),
            SyntaxKind.FragmentDefinition => RewriteFragmentDefinition(
                (FragmentDefinitionNode)node,
                context),
            SyntaxKind.IntValue => RewriteIntValue((IntValueNode)node, context),
            SyntaxKind.StringValue => RewriteStringValue((StringValueNode)node, context),
            SyntaxKind.BooleanValue => RewriteBooleanValue((BooleanValueNode)node, context),
            SyntaxKind.NullValue => RewriteNullValue((NullValueNode)node, context),
            SyntaxKind.EnumValue => RewriteEnumValue((EnumValueNode)node, context),
            SyntaxKind.ListValue => RewriteListValue((ListValueNode)node, context),
            SyntaxKind.ObjectValue => RewriteObjectValue((ObjectValueNode)node, context),
            SyntaxKind.ObjectField => RewriteObjectField((ObjectFieldNode)node, context),
            SyntaxKind.Directive => RewriteDirective((DirectiveNode)node, context),
            SyntaxKind.NamedType => RewriteNamedType((NamedTypeNode)node, context),
            SyntaxKind.ListType => RewriteListType((ListTypeNode)node, context),
            SyntaxKind.NonNullType => RewriteNonNullType((NonNullTypeNode)node, context),
            SyntaxKind.SchemaDefinition => RewriteSchemaDefinition(
                (SchemaDefinitionNode)node,
                context),
            SyntaxKind.OperationTypeDefinition => RewriteOperationTypeDefinition(
                (OperationTypeDefinitionNode)node,
                context),
            SyntaxKind.ScalarTypeDefinition => RewriteScalarTypeDefinition(
                (ScalarTypeDefinitionNode)node,
                context),
            SyntaxKind.ObjectTypeDefinition => RewriteObjectTypeDefinition(
                (ObjectTypeDefinitionNode)node,
                context),
            SyntaxKind.FieldDefinition => RewriteFieldDefinition(
                (FieldDefinitionNode)node,
                context),
            SyntaxKind.InputValueDefinition => RewriteInputValueDefinition(
                (InputValueDefinitionNode)node,
                context),
            SyntaxKind.InterfaceTypeDefinition => RewriteInterfaceTypeDefinition(
                (InterfaceTypeDefinitionNode)node,
                context),
            SyntaxKind.UnionTypeDefinition => RewriteUnionTypeDefinition(
                (UnionTypeDefinitionNode)node,
                context),
            SyntaxKind.EnumTypeDefinition => RewriteEnumTypeDefinition(
                (EnumTypeDefinitionNode)node,
                context),
            SyntaxKind.EnumValueDefinition => RewriteEnumTypeDefinition(
                (EnumTypeDefinitionNode)node,
                context),
            SyntaxKind.InputObjectTypeDefinition => RewriteInputObjectTypeDefinition(
                (InputObjectTypeDefinitionNode)node,
                context),
            SyntaxKind.SchemaExtension => RewriteSchemaExtension(
                (SchemaExtensionNode)node,
                context),
            SyntaxKind.ScalarTypeExtension => RewriteScalarTypeExtension(
                (ScalarTypeExtensionNode)node,
                context),
            SyntaxKind.ObjectTypeExtension => RewriteObjectTypeExtension(
                (ObjectTypeExtensionNode)node,
                context),
            SyntaxKind.InterfaceTypeExtension => RewriteInterfaceTypeExtension(
                (InterfaceTypeExtensionNode)node,
                context),
            SyntaxKind.UnionTypeExtension => RewriteUnionTypeExtension(
                (UnionTypeExtensionNode)node,
                context),
            SyntaxKind.EnumTypeExtension => RewriteEnumTypeExtension(
                (EnumTypeExtensionNode)node,
                context),
            SyntaxKind.InputObjectTypeExtension => RewriteInputObjectTypeExtension(
                (InputObjectTypeExtensionNode)node,
                context),
            SyntaxKind.DirectiveDefinition => RewriteDirectiveDefinition(
                (DirectiveDefinitionNode)node,
                context),
            SyntaxKind.FloatValue => RewriteFloatValue((FloatValueNode)node, context),
            _ => throw new ArgumentOutOfRangeException()
        };

    protected virtual TContext OnBeforeRewrite(ISyntaxNode node, TContext context)
        => context;

    protected virtual void OnAfterRewrite(ISyntaxNode node, TContext context)
    {
    }

    protected static TParent RewriteProp<TParent, TProperty>(
        TParent parent,
        TProperty? property,
        TContext context,
        Func<TProperty, TContext, TProperty> visit,
        Func<TProperty, TParent> rewrite)
        where TProperty : class
    {
        if (property is null)
        {
            return parent;
        }

        TProperty rewritten = visit(property, context);
        return ReferenceEquals(property, rewritten) ? parent : rewrite(rewritten);
    }

    protected static TParent RewriteMany<TParent, TProperty>(
        TParent parent,
        IReadOnlyList<TProperty> property,
        TContext context,
        Func<TProperty, TContext, TProperty> visit,
        Func<IReadOnlyList<TProperty>, TParent> rewrite)
        where TProperty : class
        => RewriteProp(parent, property, context, (p, c) => RewriteMany(p, c, visit), rewrite);

    protected static IReadOnlyList<T> RewriteMany<T>(
        IReadOnlyList<T> items,
        TContext context,
        Func<T, TContext, T> func)
    {
        IReadOnlyList<T> current = items;

        T[] rented = ArrayPool<T>.Shared.Rent(items.Count);
        Span<T> copy = rented;
        copy = copy.Slice(0, items.Count);
        var modified = false;

        for (var i = 0; i < items.Count; i++)
        {
            T original = items[i];
            T rewritten = func(items[i], context);

            copy[i] = rewritten;

            if (!modified && !ReferenceEquals(original, rewritten))
            {
                modified = true;
            }
        }

        if (modified)
        {
            var rewrittenList = new T[items.Count];

            for (int i = 0; i < items.Count; i++)
            {
                rewrittenList[i] = copy[i];
            }

            current = rewrittenList;
        }

        copy.Clear();
        ArrayPool<T>.Shared.Return(rented);

        return current;
    }

    protected virtual DocumentNode RewriteDocument(
        DocumentNode node,
        TContext context)
    {
        IReadOnlyList<IDefinitionNode> rewrittenDefinitions =
            RewriteMany(node.Definitions, context, RewriteDefinition);

        return ReferenceEquals(node.Definitions, rewrittenDefinitions)
            ? node : node.WithDefinitions(rewrittenDefinitions);
    }

    protected virtual IDefinitionNode RewriteDefinition(
        IDefinitionNode node,
        TContext context)
    {
        return node switch
        {
            OperationDefinitionNode value => RewriteOperationDefinition(value, context),
            FragmentDefinitionNode value => VisitFragmentDefinitions
                ? RewriteFragmentDefinition(value, context)
                : value,
            _ => node
        };
    }

    protected virtual OperationDefinitionNode RewriteOperationDefinition(
        OperationDefinitionNode node,
        TContext context)
    {
        OperationDefinitionNode current = node;

        if (current.Name != null)
        {
            current = RewriteProp(current, current.Name, context,
                RewriteName, current.WithName);

            current = RewriteProp(current, current.VariableDefinitions, context,
                (p, c) => RewriteMany(p, c, RewriteVariableDefinition),
                current.WithVariableDefinitions);

            current = RewriteProp(current, current.Directives, context,
                (p, c) => RewriteMany(p, c, RewriteDirective),
                current.WithDirectives);
        }

        current = RewriteProp(current, current.SelectionSet, context,
            RewriteSelectionSet, current.WithSelectionSet);

        return current;
    }

    protected virtual VariableDefinitionNode RewriteVariableDefinition(
        VariableDefinitionNode node,
        TContext context)
    {
        VariableDefinitionNode current = node;

        current = RewriteProp(current, current.Variable, context,
            RewriteVariable, current.WithVariable);

        current = RewriteProp(current, current.Type, context,
            RewriteType, current.WithType);

        if (current.DefaultValue != null)
        {
            current = RewriteProp(current, current.DefaultValue, context,
                RewriteValue, current.WithDefaultValue);
        }

        return current;
    }

    protected virtual FragmentDefinitionNode RewriteFragmentDefinition(
        FragmentDefinitionNode node,
        TContext context)
    {
        FragmentDefinitionNode current = node;

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        current = RewriteProp(current, current.TypeCondition, context,
            RewriteNamedType, current.WithTypeCondition);

        current = RewriteProp(current, current.VariableDefinitions, context,
            (p, c) => RewriteMany(p, c, RewriteVariableDefinition),
            current.WithVariableDefinitions);

        current = RewriteProp(current, current.Directives, context,
            (p, c) => RewriteMany(p, c, RewriteDirective),
            current.WithDirectives);

        current = RewriteProp(current, current.SelectionSet, context,
            RewriteSelectionSet, current.WithSelectionSet);

        return current;
    }

    protected virtual SelectionSetNode RewriteSelectionSet(
        SelectionSetNode node,
        TContext context)
    {
        SelectionSetNode current = node;

        current = RewriteProp(current, current.Selections, context,
            (p, c) => RewriteMany(p, c, RewriteSelection),
            current.WithSelections);

        return current;
    }

    protected virtual ISelectionNode RewriteSelection(
        ISelectionNode node,
        TContext context)
    {
        switch (node)
        {
            case FieldNode value:
                return RewriteField(value, context);

            case FragmentSpreadNode value:
                return RewriteFragmentSpread(value, context);

            case InlineFragmentNode value:
                return RewriteInlineFragment(value, context);

            default:
                throw new NotSupportedException();
        }
    }

    protected virtual FieldNode RewriteField(
        FieldNode node,
        TContext context)
    {
        FieldNode current = node;

        if (current.Alias != null)
        {
            current = RewriteProp(current, current.Alias, context,
                RewriteName, current.WithAlias);
        }

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        current = RewriteProp(current, current.Arguments, context,
            (p, c) => RewriteMany(p, c, RewriteArgument),
            current.WithArguments);

        current = RewriteProp(current, current.Directives, context,
            (p, c) => RewriteMany(p, c, RewriteDirective),
            current.WithDirectives);

        if (current.SelectionSet != null)
        {
            current = RewriteProp(current, current.SelectionSet, context,
                RewriteSelectionSet, current.WithSelectionSet);
        }

        return current;
    }

    protected virtual FragmentSpreadNode RewriteFragmentSpread(
        FragmentSpreadNode node,
        TContext context)
    {
        FragmentSpreadNode current = node;

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        current = RewriteProp(current, current.Directives, context,
            (p, c) => RewriteMany(p, c, RewriteDirective),
            current.WithDirectives);

        return current;
    }

    protected virtual InlineFragmentNode RewriteInlineFragment(
        InlineFragmentNode node,
        TContext context)
    {
        InlineFragmentNode current = node;

        if (current.TypeCondition != null)
        {
            current = RewriteProp(current, current.TypeCondition, context,
                RewriteNamedType, current.WithTypeCondition);
        }

        current = RewriteProp(current, current.Directives, context,
            (p, c) => RewriteMany(p, c, RewriteDirective),
            current.WithDirectives);

        current = RewriteProp(current, current.SelectionSet, context,
            RewriteSelectionSet, current.WithSelectionSet);

        return current;
    }

    protected virtual ITypeSystemDefinitionNode RewriteTypeDefinition(
        ITypeSystemDefinitionNode node,
        TContext context)
    {
        return node switch
        {
            SchemaDefinitionNode value => RewriteSchemaDefinition(value, context),
            DirectiveDefinitionNode value => RewriteDirectiveDefinition(value, context),
            ScalarTypeDefinitionNode value => RewriteScalarTypeDefinition(value, context),
            ObjectTypeDefinitionNode value => RewriteObjectTypeDefinition(value, context),
            InputObjectTypeDefinitionNode value => RewriteInputObjectTypeDefinition(value, context),
            InterfaceTypeDefinitionNode value => RewriteInterfaceTypeDefinition(value, context),
            UnionTypeDefinitionNode value => RewriteUnionTypeDefinition(value, context),
            EnumTypeDefinitionNode value => RewriteEnumTypeDefinition(value, context),
            _ => throw new NotSupportedException()
        };
    }

    protected virtual ITypeSystemExtensionNode RewriteTypeExtensionDefinition(
        ITypeSystemExtensionNode node,
        TContext context)
    {
        return node switch
        {
            SchemaExtensionNode value => RewriteSchemaExtension(value, context),
            ScalarTypeExtensionNode value => RewriteScalarTypeExtension(value, context),
            ObjectTypeExtensionNode value => RewriteObjectTypeExtension(value, context),
            InterfaceTypeExtensionNode value => RewriteInterfaceTypeExtension(value, context),
            UnionTypeExtensionNode value => RewriteUnionTypeExtension(value, context),
            EnumTypeExtensionNode value => RewriteEnumTypeExtension(value, context),
            InputObjectTypeExtensionNode value => RewriteInputObjectTypeExtension(value, context),
            _ => throw new NotSupportedException()
        };
    }

    protected virtual SchemaDefinitionNode RewriteSchemaDefinition(
        SchemaDefinitionNode node,
        TContext context)
    {
        SchemaDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteMany(current, current.OperationTypes, context,
            RewriteOperationTypeDefinition, current.WithOperationTypes);

        return current;
    }

    protected virtual SchemaExtensionNode RewriteSchemaExtension(
        SchemaExtensionNode node,
        TContext context)
    {
        SchemaExtensionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteMany(current, current.OperationTypes, context,
            RewriteOperationTypeDefinition, current.WithOperationTypes);

        return current;
    }

    protected virtual OperationTypeDefinitionNode
        RewriteOperationTypeDefinition(
        OperationTypeDefinitionNode node,
        TContext context)
    {
        OperationTypeDefinitionNode current = node;

        current = RewriteProp(current, current.Type, context,
            RewriteNamedType, current.WithType);

        return current;
    }

    protected virtual DirectiveDefinitionNode RewriteDirectiveDefinition(
        DirectiveDefinitionNode node,
        TContext context)
    {
        DirectiveDefinitionNode current = node;

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(current, current.Description, context,
                RewriteStringValue, current.WithDescription);
        }

        current = RewriteMany(current, current.Arguments, context,
            RewriteInputValueDefinition, current.WithArguments);

        current = RewriteMany(current, current.Locations, context,
            RewriteName, current.WithLocations);

        return current;
    }

    protected virtual ScalarTypeDefinitionNode RewriteScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        TContext context)
    {
        ScalarTypeDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(current, current.Description, context,
                RewriteStringValue, current.WithDescription);
        }

        return current;
    }

    protected virtual ScalarTypeExtensionNode RewriteScalarTypeExtension(
        ScalarTypeExtensionNode node,
        TContext context)
    {
        ScalarTypeExtensionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        return current;
    }

    protected virtual ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        TContext context)
    {
        ObjectTypeDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(current, current.Description, context,
                RewriteStringValue, current.WithDescription);
        }

        current = RewriteMany(current, current.Interfaces, context,
            RewriteNamedType, current.WithInterfaces);

        current = RewriteMany(current, current.Fields, context,
            RewriteFieldDefinition, current.WithFields);

        return current;
    }

    protected virtual ObjectTypeExtensionNode RewriteObjectTypeExtension(
        ObjectTypeExtensionNode node,
        TContext context)
    {
        ObjectTypeExtensionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        current = RewriteMany(current, current.Interfaces, context,
            RewriteNamedType, current.WithInterfaces);

        current = RewriteMany(current, current.Fields, context,
            RewriteFieldDefinition, current.WithFields);

        current = RewriteMany(current, current.Fields, context,
            RewriteFieldDefinition, current.WithFields);

        return current;
    }

    protected virtual FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        TContext context)
    {
        FieldDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(current, current.Description, context,
               RewriteStringValue, current.WithDescription);
        }

        current = RewriteMany(current, current.Arguments, context,
            RewriteInputValueDefinition, current.WithArguments);

        current = RewriteProp(current, current.Type, context,
            RewriteType, current.WithType);

        return current;
    }

    protected virtual InputObjectTypeDefinitionNode
        RewriteInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            TContext context)
    {
        InputObjectTypeDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(current, current.Description, context,
                RewriteStringValue, current.WithDescription);
        }

        current = RewriteMany(current, current.Fields, context,
            RewriteInputValueDefinition, current.WithFields);

        return current;
    }

    protected virtual InputObjectTypeExtensionNode
        RewriteInputObjectTypeExtension(
            InputObjectTypeExtensionNode node,
            TContext context)
    {
        InputObjectTypeExtensionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        current = RewriteMany(current, current.Fields, context,
            RewriteInputValueDefinition, current.WithFields);

        return current;
    }

    protected virtual InputValueDefinitionNode RewriteInputValueDefinition(
        InputValueDefinitionNode node,
        TContext context)
    {
        InputValueDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(current, current.Description, context,
                RewriteStringValue, current.WithDescription);
        }

        current = RewriteProp(current, current.Type, context,
            RewriteType, current.WithType);

        if (current.DefaultValue is not null)
        {
            current = RewriteProp(current, current.DefaultValue, context,
                RewriteValue, current.WithDefaultValue);
        }

        return current;
    }

    protected virtual InterfaceTypeDefinitionNode
        RewriteInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node,
            TContext context)
    {
        InterfaceTypeDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(
                current,
                current.Description,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = RewriteMany(current, current.Fields, context,
            RewriteFieldDefinition, current.WithFields);

        return current;
    }

    protected virtual InterfaceTypeExtensionNode
        RewriteInterfaceTypeExtension(
            InterfaceTypeExtensionNode node,
            TContext context)
    {
        InterfaceTypeExtensionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        current = RewriteMany(current, current.Fields, context,
            RewriteFieldDefinition, current.WithFields);

        return current;
    }

    protected virtual UnionTypeDefinitionNode RewriteUnionTypeDefinition(
        UnionTypeDefinitionNode node,
        TContext context)
    {
        UnionTypeDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(current, current.Description, context,
                RewriteStringValue, current.WithDescription);
        }

        current = RewriteMany(current, current.Types, context,
            RewriteNamedType, current.WithTypes);

        return current;
    }

    protected virtual UnionTypeExtensionNode RewriteUnionTypeExtension(
        UnionTypeExtensionNode node,
        TContext context)
    {
        UnionTypeExtensionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        current = RewriteMany(current, current.Types, context,
            RewriteNamedType, current.WithTypes);

        return current;
    }

    protected virtual EnumTypeDefinitionNode RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        TContext context)
    {
        EnumTypeDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(current, current.Description, context,
                RewriteStringValue, current.WithDescription);
        }

        current = RewriteMany(current, current.Values, context,
            RewriteEnumValueDefinition, current.WithValues);

        return current;
    }

    protected virtual EnumTypeExtensionNode RewriteEnumTypeExtension(
        EnumTypeExtensionNode node,
        TContext context)
    {
        EnumTypeExtensionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        current = RewriteMany(current, current.Values, context,
            RewriteEnumValueDefinition, current.WithValues);

        return current;
    }

    protected virtual EnumValueDefinitionNode RewriteEnumValueDefinition(
       EnumValueDefinitionNode node,
       TContext context)
    {
        EnumValueDefinitionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            context, current.WithDirectives);

        current = RewriteProp(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = RewriteProp(
                current,
                current.Description,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        return current;
    }

    protected virtual NameNode RewriteName(NameNode node, TContext context)
        => node;

    protected virtual VariableNode RewriteVariable(VariableNode node, TContext context)
    {
        VariableNode current = node;

        current = RewriteProp(
            current,
            node.Name,
            context,
            (n, c) => RewriteName(n, c),
            n => current.WithName(n));

        return current;
    }

    protected virtual ArgumentNode RewriteArgument(
        ArgumentNode node,
        TContext context)
    {
        ArgumentNode current = node;

        current = RewriteProp(
            current,
            node.Name,
            context,
            (n, c) => RewriteName(n, c),
            n => current.WithName(n));

        current = RewriteProp(
            current,
            node.Value,
            context,
            (n, c) => RewriteValue(n, c),
            n => current.WithValue(n));

        return current;
    }

    protected virtual IntValueNode RewriteIntValue(IntValueNode node, TContext context)
        => node;

    protected virtual FloatValueNode RewriteFloatValue(FloatValueNode node, TContext context)
        => node;

    protected virtual StringValueNode RewriteStringValue(StringValueNode node, TContext context)
        => node;

    protected virtual BooleanValueNode RewriteBooleanValue(BooleanValueNode node, TContext context)
        => node;

    protected virtual EnumValueNode RewriteEnumValue(EnumValueNode node, TContext context)
        => node;

    protected virtual NullValueNode RewriteNullValue(NullValueNode node, TContext context)
        => node;

    protected virtual ListValueNode RewriteListValue(ListValueNode node, TContext context)
    {
        ListValueNode current = node;

        current = RewriteMany(
            current,
            current.Items,
            context,
            (n, c) => RewriteValue(n, c),
            n => current.WithItems(n));

        return current;
    }

    protected virtual ObjectValueNode RewriteObjectValue(ObjectValueNode node, TContext context)
    {
        ObjectValueNode current = node;

        current = RewriteMany(
            current,
            current.Fields,
            context,
            (n, c) => RewriteObjectField(n, c),
            n => current.WithFields(n));

        return current;
    }

    protected virtual ObjectFieldNode RewriteObjectField(ObjectFieldNode node, TContext context)
    {
        ObjectFieldNode current = node;

        current = RewriteProp(
            current,
            node.Name,
            context,
            (n, c) => RewriteName(n, c),
            n => current.WithName(n));

        current = RewriteProp(
            current,
            node.Value,
            context,
            (n, c) => RewriteValue(n, c),
            n => current.WithValue(n));

        return current;
    }

    protected virtual DirectiveNode RewriteDirective(DirectiveNode node, TContext context)
        => node;

    protected virtual TParent RewriteDirectives<TParent>(
        TParent parent,
        IReadOnlyList<DirectiveNode> directives,
        TContext context,
        Func<IReadOnlyList<DirectiveNode>, TParent> rewrite)
        => RewriteMany(parent, directives, context, (n, c) => RewriteDirective(n, c), rewrite);

    protected virtual NamedTypeNode RewriteNamedType(NamedTypeNode node, TContext context)
    {
        NamedTypeNode current = node;

        current = RewriteProp(
            current,
            node.Name,
            context,
            (n, c) => RewriteName(n, c),
            n => current.WithName(n));

        return current;
    }

    protected virtual ListTypeNode RewriteListType(ListTypeNode node, TContext context)
    {
        ListTypeNode current = node;

        current = RewriteProp(
            current,
            current.Type,
            context,
            (n, c) => RewriteType(n, c),
            n => current.WithType(n));

        return current;
    }

    protected virtual NonNullTypeNode RewriteNonNullType(NonNullTypeNode node, TContext context)
    {
        NonNullTypeNode current = node;

        current = RewriteProp(
            current,
            current.Type,
            context,
            (t, c) => (INullableTypeNode)RewriteType(t, c),
            n => current.WithType(n));

        return current;
    }

    protected virtual IValueNode RewriteValue(IValueNode node, TContext context)
        => node switch
        {
            IntValueNode value => RewriteIntValue(value, context),
            FloatValueNode value => RewriteFloatValue(value, context),
            StringValueNode value => RewriteStringValue(value, context),
            BooleanValueNode value => RewriteBooleanValue(value, context),
            EnumValueNode value => RewriteEnumValue(value, context),
            NullValueNode value => RewriteNullValue(value, context),
            ListValueNode value => RewriteListValue(value, context),
            ObjectValueNode value => RewriteObjectValue(value, context),
            VariableNode value => RewriteVariable(value, context),
            _ => throw new NotSupportedException()
        };

    protected virtual ITypeNode RewriteType(ITypeNode node, TContext context)
        => node switch
        {
            NonNullTypeNode value => RewriteNonNullType(value, context),
            ListTypeNode value => RewriteListType(value, context),
            NamedTypeNode value => RewriteNamedType(value, context),
            _ => throw new NotSupportedException()
        };
}
