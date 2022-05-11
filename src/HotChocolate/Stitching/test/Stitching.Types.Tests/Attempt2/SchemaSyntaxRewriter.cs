using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public class SchemaSyntaxRewriter<TContext>
    : SyntaxRewriter<TContext>
{
    public virtual ISyntaxNode Rewrite(ISyntaxNode node, TContext context)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var navigator = new SyntaxNavigator();
        using IDisposable _ = navigator.Push(node);

        switch (node)
        {
            case DocumentNode document:
                return RewriteDocument(document, navigator, context);

            case ITypeSystemExtensionNode extension:
                return RewriteTypeExtensionDefinition(extension, navigator, context);

            case ITypeSystemDefinitionNode definition:
                return RewriteTypeDefinition(definition, navigator, context);

            default:
                throw new NotSupportedException();
        }
    }

    protected virtual DocumentNode RewriteDocument(DocumentNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        IReadOnlyList<IDefinitionNode> rewrittenDefinitions =
            RewriteMany(node.Definitions, navigator, context, (n, nav, c) =>
            {
                if (n is ITypeSystemExtensionNode extension)
                {
                    return RewriteTypeExtensionDefinition(extension, nav, c);
                }

                if (n is ITypeSystemDefinitionNode definition)
                {
                    return RewriteTypeDefinition(definition, nav, c);
                }

                throw new NotSupportedException();
            });

        return ReferenceEquals(node.Definitions, rewrittenDefinitions)
            ? node : node.WithDefinitions(rewrittenDefinitions);
    }

    protected virtual ITypeSystemDefinitionNode RewriteTypeDefinition(ITypeSystemDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        switch (node)
        {
            case SchemaDefinitionNode value:
                return RewriteSchemaDefinition(value, navigator, context);

            case DirectiveDefinitionNode value:
                return RewriteDirectiveDefinition(value, navigator, context);

            case ScalarTypeDefinitionNode value:
                return RewriteScalarTypeDefinition(value, navigator, context);

            case ObjectTypeDefinitionNode value:
                return RewriteObjectTypeDefinition(value, navigator, context);

            case InputObjectTypeDefinitionNode value:
                return RewriteInputObjectTypeDefinition(value, navigator, context);

            case InterfaceTypeDefinitionNode value:
                return RewriteInterfaceTypeDefinition(value, navigator, context);

            case UnionTypeDefinitionNode value:
                return RewriteUnionTypeDefinition(value, navigator, context);

            case EnumTypeDefinitionNode value:
                return RewriteEnumTypeDefinition(value, navigator, context);

            default:
                throw new NotSupportedException();
        }
    }

    protected virtual ITypeSystemExtensionNode RewriteTypeExtensionDefinition(
        ITypeSystemExtensionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        switch (node)
        {
            case SchemaExtensionNode value:
                return RewriteSchemaExtension(value, navigator, context);

            case ScalarTypeExtensionNode value:
                return RewriteScalarTypeExtension(value, navigator, context);

            case ObjectTypeExtensionNode value:
                return RewriteObjectTypeExtension(value, navigator, context);

            case InterfaceTypeExtensionNode value:
                return RewriteInterfaceTypeExtension(value, navigator, context);

            case UnionTypeExtensionNode value:
                return RewriteUnionTypeExtension(value, navigator, context);

            case EnumTypeExtensionNode value:
                return RewriteEnumTypeExtension(value, navigator, context);

            case InputObjectTypeExtensionNode value:
                return RewriteInputObjectTypeExtension(value, navigator, context);

            default:
                throw new NotSupportedException();
        }
    }

    protected virtual SchemaDefinitionNode RewriteSchemaDefinition(
        SchemaDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        SchemaDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = RewriteMany(current,
            current.OperationTypes,
            navigator,
            context,
            RewriteOperationTypeDefinition,
            current.WithOperationTypes);

        return current;
    }

    protected virtual SchemaExtensionNode RewriteSchemaExtension(
        SchemaExtensionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        SchemaExtensionNode current = node;

        current = RewriteDirectives(current, current.Directives,
            navigator,
            context, current.WithDirectives);

        current = RewriteMany(current, current.OperationTypes,
            navigator,
            context,
            RewriteOperationTypeDefinition, current.WithOperationTypes);

        return current;
    }

    protected virtual OperationTypeDefinitionNode
        RewriteOperationTypeDefinition(
            OperationTypeDefinitionNode node,
            SyntaxNavigator navigator,
            TContext context)
    {
        OperationTypeDefinitionNode current = node;

        current = Rewrite(current, current.Type, navigator, context,
            RewriteNamedType, current.WithType);

        return current;
    }

    protected virtual DirectiveDefinitionNode RewriteDirectiveDefinition(
        DirectiveDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        DirectiveDefinitionNode current = node;

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = RewriteMany(current,
            current.Arguments,
            navigator,
            context,
            RewriteInputValueDefinition,
            current.WithArguments);

        current = RewriteMany(current,
            current.Locations,
            navigator,
            context,
            RewriteName,
            current.WithLocations);

        return current;
    }

    protected virtual ScalarTypeDefinitionNode RewriteScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ScalarTypeDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        return current;
    }

    protected virtual ScalarTypeExtensionNode RewriteScalarTypeExtension(
        ScalarTypeExtensionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ScalarTypeExtensionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        return current;
    }

    protected virtual ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ObjectTypeDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = RewriteMany(current,
            current.Interfaces,
            navigator,
            context,
            RewriteNamedType,
            current.WithInterfaces);

        current = RewriteMany(current,
            current.Fields,
            navigator,
            context,
            RewriteFieldDefinition,
            current.WithFields);

        return current;
    }

    protected virtual ObjectTypeExtensionNode RewriteObjectTypeExtension(
        ObjectTypeExtensionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        ObjectTypeExtensionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        current = RewriteMany(current,
            current.Interfaces,
            navigator,
            context,
            RewriteNamedType,
            current.WithInterfaces);

        current = RewriteMany(current,
            current.Fields,
            navigator,
            context,
            RewriteFieldDefinition,
            current.WithFields);

        current = RewriteMany(current,
            current.Fields,
            navigator,
            context,
            RewriteFieldDefinition,
            current.WithFields);

        return current;
    }

    protected virtual FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        FieldDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = RewriteMany(current,
            current.Arguments,
            navigator,
            context,
            RewriteInputValueDefinition,
            current.WithArguments);

        current = Rewrite(current,
            current.Type,
            navigator,
            context,
            RewriteType,
            current.WithType);

        return current;
    }

    protected virtual InputObjectTypeDefinitionNode
        RewriteInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            SyntaxNavigator navigator,
            TContext context)
    {
        InputObjectTypeDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = RewriteMany(current,
            current.Fields,
            navigator,
            context,
            RewriteInputValueDefinition,
            current.WithFields);

        return current;
    }

    protected virtual InputObjectTypeExtensionNode
        RewriteInputObjectTypeExtension(
            InputObjectTypeExtensionNode node,
            SyntaxNavigator navigator,
            TContext context)
    {
        InputObjectTypeExtensionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        current = RewriteMany(current,
            current.Fields,
            navigator,
            context,
            RewriteInputValueDefinition,
            current.WithFields);

        return current;
    }

    protected virtual InputValueDefinitionNode RewriteInputValueDefinition(
        InputValueDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        InputValueDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = Rewrite(current,
            current.Type,
            navigator,
            context,
            RewriteType,
            current.WithType);

        if (current.DefaultValue is not null)
        {
            current = Rewrite(current,
                current.DefaultValue,
                navigator,
                context,
                RewriteValue,
                current.WithDefaultValue);
        }

        return current;
    }

    protected virtual InterfaceTypeDefinitionNode
        RewriteInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node,
            SyntaxNavigator navigator,
            TContext context)
    {
        InterfaceTypeDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(
                current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = RewriteMany(current,
            current.Fields,
            navigator,
            context,
            RewriteFieldDefinition,
            current.WithFields);

        return current;
    }

    protected virtual InterfaceTypeExtensionNode
        RewriteInterfaceTypeExtension(
            InterfaceTypeExtensionNode node,
            SyntaxNavigator navigator,
            TContext context)
    {
        InterfaceTypeExtensionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        current = RewriteMany(current,
            current.Fields,
            navigator,
            context,
            RewriteFieldDefinition,
            current.WithFields);

        return current;
    }

    protected virtual UnionTypeDefinitionNode RewriteUnionTypeDefinition(
        UnionTypeDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        UnionTypeDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = RewriteMany(current,
            current.Types,
            navigator,
            context,
            RewriteNamedType,
            current.WithTypes);

        return current;
    }

    protected virtual UnionTypeExtensionNode RewriteUnionTypeExtension(
        UnionTypeExtensionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        UnionTypeExtensionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        current = RewriteMany(current,
            current.Types,
            navigator,
            context,
            RewriteNamedType,
            current.WithTypes);

        return current;
    }

    protected virtual EnumTypeDefinitionNode RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        EnumTypeDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        current = RewriteMany(current,
            current.Values,
            navigator,
            context,
            RewriteEnumValueDefinition,
            current.WithValues);

        return current;
    }

    protected virtual EnumTypeExtensionNode RewriteEnumTypeExtension(
        EnumTypeExtensionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        EnumTypeExtensionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        current = RewriteMany(current,
            current.Values,
            navigator,
            context,
            RewriteEnumValueDefinition,
            current.WithValues);

        return current;
    }

    protected virtual EnumValueDefinitionNode RewriteEnumValueDefinition(
        EnumValueDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        EnumValueDefinitionNode current = node;

        current = RewriteDirectives(current,
            current.Directives,
            navigator,
            context,
            current.WithDirectives);

        current = Rewrite(current,
            current.Name,
            navigator,
            context,
            RewriteName,
            current.WithName);

        if (current.Description is not null)
        {
            current = Rewrite(
                current,
                current.Description,
                navigator,
                context,
                RewriteStringValue,
                current.WithDescription);
        }

        return current;
    }
}
