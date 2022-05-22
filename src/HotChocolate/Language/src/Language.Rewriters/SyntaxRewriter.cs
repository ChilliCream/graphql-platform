using System;
using System.Buffers;
using System.Collections.Generic;

namespace HotChocolate.Language;

public class SyntaxRewriter<TContext>
{
    public virtual ISyntaxNode Rewrite(ISyntaxNode node, TContext context)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return node switch
        {
            DocumentNode doc => RewriteDocument(doc, context),
            ITypeSystemExtensionNode ext => RewriteTypeExtensionDefinition(ext, context),
            ITypeSystemDefinitionNode def => RewriteTypeDefinition(def, context),
            _ => throw new NotSupportedException()
        };
    }

    protected virtual DocumentNode RewriteDocument(DocumentNode node, TContext context)
    {
        IReadOnlyList<IDefinitionNode> rewrittenDefinitions =
            RewriteMany(
                node.Definitions,
                context,
                (n, c) =>
                {
                    if (n is ITypeSystemExtensionNode extension)
                    {
                        return RewriteTypeExtensionDefinition(extension, c);
                    }

                    if (n is ITypeSystemDefinitionNode definition)
                    {
                        return RewriteTypeDefinition(definition, c);
                    }

                    throw new NotSupportedException();
                });

        return ReferenceEquals(node.Definitions, rewrittenDefinitions)
            ? node
            : node.WithDefinitions(rewrittenDefinitions);
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

        current = Rewrite(current, current.Type, context,
            RewriteNamedType, current.WithType);

        return current;
    }

    protected virtual DirectiveDefinitionNode RewriteDirectiveDefinition(
        DirectiveDefinitionNode node,
        TContext context)
    {
        DirectiveDefinitionNode current = node;

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current, current.Description, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current, current.Description, context,
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

        current = Rewrite(current, current.Name, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current, current.Description, context,
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

        current = Rewrite(current, current.Name, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current, current.Description, context,
               RewriteStringValue, current.WithDescription);
        }

        current = RewriteMany(current, current.Arguments, context,
            RewriteInputValueDefinition, current.WithArguments);

        current = Rewrite(current, current.Type, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current, current.Description, context,
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

        current = Rewrite(current, current.Name, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current, current.Description, context,
                RewriteStringValue, current.WithDescription);
        }

        current = Rewrite(current, current.Type, context,
            RewriteType, current.WithType);

        if (current.DefaultValue is not null)
        {
            current = Rewrite(current, current.DefaultValue, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(
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

        current = Rewrite(current, current.Name, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current, current.Description, context,
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

        current = Rewrite(current, current.Name, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current, current.Description, context,
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

        current = Rewrite(current, current.Name, context,
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

        current = Rewrite(current, current.Name, context,
            RewriteName, current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(
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

        current = Rewrite(
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

        current = Rewrite(
            current,
            node.Name,
            context,
            (n, c) => RewriteName(n, c),
            n => current.WithName(n));

        current = Rewrite(
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

        current = Rewrite(
            current,
            node.Name,
            context,
            (n, c) => RewriteName(n, c),
            n => current.WithName(n));

        current = Rewrite(
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

        current = Rewrite(
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

        current = Rewrite(
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

        current = Rewrite(
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

    protected static TParent Rewrite<TParent, TProperty>(
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
        => Rewrite(parent, property, context, (p, c) => RewriteMany(p, c, visit), rewrite);

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
}
