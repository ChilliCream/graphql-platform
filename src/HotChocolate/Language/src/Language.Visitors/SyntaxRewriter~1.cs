namespace HotChocolate.Language.Visitors;

/// <summary>
/// Represents a syntax rewriter. A syntax rewriter is a visitor that creates a new syntax tree
/// from the passed in syntax tree.
/// </summary>
/// <typeparam name="TContext">
/// The context type.
/// </typeparam>
public class SyntaxRewriter<TContext> : ISyntaxRewriter<TContext>
{
    public virtual ISyntaxNode? Rewrite(ISyntaxNode node, TContext context)
    {
        var newContext = OnEnter(node, context);
        var rewrittenNode = OnRewrite(node, context);
        OnLeave(rewrittenNode, newContext);
        return rewrittenNode;
    }

    protected virtual TContext OnEnter(ISyntaxNode node, TContext context)
        => context;

    protected virtual ISyntaxNode? OnRewrite(ISyntaxNode node, TContext context)
        => node switch
        {
            ArgumentNode n => RewriteArgument(n, context),
            BooleanValueNode n => RewriteBooleanValue(n, context),
            DirectiveDefinitionNode n => RewriteDirectiveDefinition(n, context),
            DirectiveNode n => RewriteDirective(n, context),
            DocumentNode n => RewriteDocument(n, context),
            EnumTypeDefinitionNode n => RewriteEnumTypeDefinition(n, context),
            EnumTypeExtensionNode n => RewriteEnumTypeExtension(n, context),
            EnumValueDefinitionNode n => RewriteEnumValueDefinition(n, context),
            EnumValueNode n => RewriteEnumValue(n, context),
            FieldDefinitionNode n => RewriteFieldDefinition(n, context),
            FieldNode n => RewriteField(n, context),
            FloatValueNode n => RewriteFloatValue(n, context),
            FragmentDefinitionNode n => RewriteFragmentDefinition(n, context),
            FragmentSpreadNode n => RewriteFragmentSpread(n, context),
            InlineFragmentNode n => RewriteInlineFragment(n, context),
            InputObjectTypeDefinitionNode n => RewriteInputObjectTypeDefinition(n, context),
            InputObjectTypeExtensionNode n => RewriteInputObjectTypeExtension(n, context),
            InputValueDefinitionNode n => RewriteInputValueDefinition(n, context),
            InterfaceTypeDefinitionNode n => RewriteInterfaceTypeDefinition(n, context),
            InterfaceTypeExtensionNode n => RewriteInterfaceTypeExtension(n, context),
            IntValueNode n => RewriteIntValue(n, context),
            ListTypeNode n => RewriteListType(n, context),
            ListValueNode n => RewriteListValue(n, context),
            NamedTypeNode n => RewriteNamedType(n, context),
            NameNode n => RewriteName(n, context),
            NonNullTypeNode n => RewriteNonNullType(n, context),
            NullValueNode n => RewriteNullValue(n, context),
            ObjectFieldNode n => RewriteObjectField(n, context),
            ObjectTypeDefinitionNode n => RewriteObjectTypeDefinition(n, context),
            ObjectTypeExtensionNode n => RewriteObjectTypeExtension(n, context),
            ObjectValueNode n => RewriteObjectValue(n, context),
            OperationDefinitionNode n => RewriteOperationDefinition(n, context),
            OperationTypeDefinitionNode n => RewriteOperationTypeDefinition(n, context),
            ScalarTypeDefinitionNode n => RewriteScalarTypeDefinition(n, context),
            ScalarTypeExtensionNode n => RewriteScalarTypeExtension(n, context),
            SchemaCoordinateNode n => RewriteSchemaCoordinate(n, context),
            SchemaDefinitionNode n => RewriteSchemaDefinition(n, context),
            SchemaExtensionNode n => RewriteSchemaExtension(n, context),
            SelectionSetNode n => RewriteSelectionSet(n, context),
            StringValueNode n => RewriteStringValue(n, context),
            UnionTypeDefinitionNode n => RewriteUnionTypeDefinition(n, context),
            UnionTypeExtensionNode n => RewriteUnionTypeExtension(n, context),
            VariableDefinitionNode n => RewriteVariableDefinition(n, context),
            VariableNode n => RewriteVariable(n, context),
            IValueNode n => RewriteCustomValue(n, context),
            _ => throw new ArgumentOutOfRangeException(nameof(node)),
        };

    protected virtual void OnLeave(
        ISyntaxNode? node,
        TContext context)
    {
    }

    protected virtual ArgumentNode? RewriteArgument(
        ArgumentNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var value = RewriteNode(node.Value, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(value, node.Value))
        {
            return new ArgumentNode(node.Location, name, value);
        }

        return node;
    }

    protected virtual BooleanValueNode? RewriteBooleanValue(
        BooleanValueNode node,
        TContext context)
        => node;

    protected virtual DirectiveDefinitionNode? RewriteDirectiveDefinition(
        DirectiveDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var arguments = RewriteList(node.Arguments, context);
        var locations = RewriteList(node.Locations, context);

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

    protected virtual DirectiveNode? RewriteDirective(
        DirectiveNode node,
        TContext context)
        => node;

    protected virtual DocumentNode? RewriteDocument(
        DocumentNode node,
        TContext context)
    {
        var definitions = RewriteList(node.Definitions, context);

        if (!ReferenceEquals(definitions, node.Definitions))
        {
            return new DocumentNode(node.Location, definitions);
        }

        return node;
    }

    protected virtual EnumTypeDefinitionNode? RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var directives = RewriteList(node.Directives, context);
        var values = RewriteList(node.Values, context);

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

    protected virtual EnumTypeExtensionNode? RewriteEnumTypeExtension(
        EnumTypeExtensionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var directives = RewriteList(node.Directives, context);
        var values = RewriteList(node.Values, context);

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

    protected virtual EnumValueDefinitionNode? RewriteEnumValueDefinition(
        EnumValueDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var directives = RewriteList(node.Directives, context);

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

    protected virtual EnumValueNode? RewriteEnumValue(
        EnumValueNode node,
        TContext context)
        => node;

    protected virtual FieldDefinitionNode? RewriteFieldDefinition(
        FieldDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var arguments = RewriteList(node.Arguments, context);
        var type = RewriteNode(node.Type, context);
        var directives = RewriteList(node.Directives, context);

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

    protected virtual FieldNode? RewriteField(
        FieldNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var alias = RewriteNodeOrDefault(node.Alias, context);
        var directives = RewriteList(node.Directives, context);
        var arguments = RewriteList(node.Arguments, context);
        var selectionSet = RewriteNodeOrDefault(node.SelectionSet, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(alias, node.Alias) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(arguments, node.Arguments) ||
            !ReferenceEquals(selectionSet, node.SelectionSet))
        {
            return new FieldNode(
                node.Location,
                name,
                alias,
                directives,
                arguments,
                selectionSet);
        }

        return node;
    }

    protected virtual FloatValueNode? RewriteFloatValue(
        FloatValueNode node,
        TContext context)
        => node;

    protected virtual FragmentDefinitionNode? RewriteFragmentDefinition(
        FragmentDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var variableDefinitions = RewriteList(node.VariableDefinitions, context);
        var typeCondition = RewriteNode(node.TypeCondition, context);
        var directives = RewriteList(node.Directives, context);
        var selectionSet = RewriteNode(node.SelectionSet, context);

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

    protected virtual FragmentSpreadNode? RewriteFragmentSpread(
        FragmentSpreadNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var directives = RewriteList(node.Directives, context);

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

    protected virtual InlineFragmentNode? RewriteInlineFragment(
        InlineFragmentNode node,
        TContext context)
    {
        var typeCondition = RewriteNodeOrDefault(node.TypeCondition, context);
        var directives = RewriteList(node.Directives, context);
        var selectionSet = RewriteNode(node.SelectionSet, context);

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

    protected virtual InputObjectTypeDefinitionNode? RewriteInputObjectTypeDefinition(
        InputObjectTypeDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var directives = RewriteList(node.Directives, context);
        var fields = RewriteList(node.Fields, context);

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

    protected virtual InputObjectTypeExtensionNode? RewriteInputObjectTypeExtension(
        InputObjectTypeExtensionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var directives = RewriteList(node.Directives, context);
        var fields = RewriteList(node.Fields, context);

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

    protected virtual InputValueDefinitionNode? RewriteInputValueDefinition(
        InputValueDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var type = RewriteNode(node.Type, context);
        var defaultValue = RewriteNodeOrDefault(node.DefaultValue, context);
        var directives = RewriteList(node.Directives, context);

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

    protected virtual InterfaceTypeDefinitionNode? RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var directives = RewriteList(node.Directives, context);
        var interfaces = RewriteList(node.Interfaces, context);
        var fields = RewriteList(node.Fields, context);

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

    protected virtual InterfaceTypeExtensionNode? RewriteInterfaceTypeExtension(
        InterfaceTypeExtensionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var directives = RewriteList(node.Directives, context);
        var interfaces = RewriteList(node.Interfaces, context);
        var fields = RewriteList(node.Fields, context);

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

    protected virtual IntValueNode? RewriteIntValue(
        IntValueNode node,
        TContext context)
        => node;

    protected virtual ListTypeNode? RewriteListType(
        ListTypeNode node,
        TContext context)
    {
        var type = RewriteNode(node.Type, context);

        if (!ReferenceEquals(type, node.Type))
        {
            return new ListTypeNode(node.Location, type);
        }

        return node;
    }

    protected virtual ListValueNode? RewriteListValue(
        ListValueNode node,
        TContext context)
    {
        var items = RewriteList(node.Items, context);

        if (!ReferenceEquals(items, node.Items))
        {
            return new ListValueNode(node.Location, items);
        }

        return node;
    }

    protected virtual NamedTypeNode? RewriteNamedType(
        NamedTypeNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);

        if (!ReferenceEquals(name, node.Name))
        {
            return new NamedTypeNode(node.Location, name);
        }

        return node;
    }

    protected virtual NameNode? RewriteName(
        NameNode node,
        TContext context)
        => node;

    protected virtual NonNullTypeNode? RewriteNonNullType(
        NonNullTypeNode node,
        TContext context)
    {
        var type = RewriteNode(node.Type, context);

        if (!ReferenceEquals(type, node.Type))
        {
            return new NonNullTypeNode(node.Location, type);
        }

        return node;
    }

    protected virtual NullValueNode? RewriteNullValue(
        NullValueNode node,
        TContext context)
        => node;

    protected virtual ObjectFieldNode? RewriteObjectField(
        ObjectFieldNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var value = RewriteNode(node.Value, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(value, node.Value))
        {
            return new ObjectFieldNode(node.Location, name, value);
        }

        return node;
    }

    protected virtual ObjectTypeDefinitionNode? RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var directives = RewriteList(node.Directives, context);
        var interfaces = RewriteList(node.Interfaces, context);
        var fields = RewriteList(node.Fields, context);

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

    protected virtual ObjectTypeExtensionNode? RewriteObjectTypeExtension(
        ObjectTypeExtensionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var directives = RewriteList(node.Directives, context);
        var interfaces = RewriteList(node.Interfaces, context);
        var fields = RewriteList(node.Fields, context);

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

    protected virtual ObjectValueNode? RewriteObjectValue(
        ObjectValueNode node,
        TContext context)
    {
        var fields = RewriteList(node.Fields, context);

        if (!ReferenceEquals(fields, node.Fields))
        {
            return new ObjectValueNode(node.Location, fields);
        }

        return node;
    }

    protected virtual OperationDefinitionNode? RewriteOperationDefinition(
        OperationDefinitionNode node,
        TContext context)
    {
        var name = RewriteNodeOrDefault(node.Name, context);
        var variableDefinitions = RewriteList(node.VariableDefinitions, context);
        var directives = RewriteList(node.Directives, context);
        var selectionSet = RewriteNode(node.SelectionSet, context);

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

    protected virtual OperationTypeDefinitionNode? RewriteOperationTypeDefinition(
        OperationTypeDefinitionNode node,
        TContext context)
    {
        var type = RewriteNode(node.Type, context);

        if (!ReferenceEquals(type, node.Type))
        {
            return new OperationTypeDefinitionNode(
                node.Location,
                node.Operation,
                type);
        }

        return node;
    }

    protected virtual ScalarTypeDefinitionNode? RewriteScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var directives = RewriteList(node.Directives, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(directives, node.Directives))
        {
            return new ScalarTypeDefinitionNode(
                node.Location,
                name,
                description,
                directives);
        }

        return node;
    }

    protected virtual ScalarTypeExtensionNode? RewriteScalarTypeExtension(
        ScalarTypeExtensionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var directives = RewriteList(node.Directives, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(directives, node.Directives))
        {
            return new ScalarTypeExtensionNode(
                node.Location,
                name,
                directives);
        }

        return node;
    }

    protected virtual SchemaCoordinateNode? RewriteSchemaCoordinate(
        SchemaCoordinateNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var memberName = RewriteNodeOrDefault(node.MemberName, context);
        var argumentName = RewriteNodeOrDefault(node.ArgumentName, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(memberName, node.MemberName) ||
            !ReferenceEquals(argumentName, node.ArgumentName))
        {
            return new SchemaCoordinateNode(
                node.Location,
                node.OfDirective,
                name,
                memberName,
                argumentName);
        }

        return node;
    }

    protected virtual SchemaDefinitionNode? RewriteSchemaDefinition(
        SchemaDefinitionNode node,
        TContext context)
    {
        var description = RewriteNodeOrDefault(node.Description, context);
        var directives = RewriteList(node.Directives, context);
        var operationTypes =
            RewriteList(node.OperationTypes, context);

        if (!ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(operationTypes, node.OperationTypes))
        {
            return new SchemaDefinitionNode(
                node.Location,
                description,
                directives,
                operationTypes);
        }

        return node;
    }

    protected virtual SchemaExtensionNode? RewriteSchemaExtension(
        SchemaExtensionNode node,
        TContext context)
    {
        var directives = RewriteList(node.Directives, context);
        var operationTypes =
            RewriteList(node.OperationTypes, context);

        if (!ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(operationTypes, node.OperationTypes))
        {
            return new SchemaExtensionNode(
                node.Location,
                directives,
                operationTypes);
        }

        return node;
    }

    protected virtual SelectionSetNode? RewriteSelectionSet(
        SelectionSetNode node,
        TContext context)
    {
        var selections = RewriteList(node.Selections, context);

        if (!ReferenceEquals(selections, node.Selections))
        {
            return new SelectionSetNode(node.Location, selections);
        }

        return node;
    }

    protected virtual StringValueNode? RewriteStringValue(
        StringValueNode node,
        TContext context)
        => node;

    protected virtual IValueNode? RewriteCustomValue(
        IValueNode node,
        TContext context)
        => node;

    protected virtual UnionTypeDefinitionNode? RewriteUnionTypeDefinition(
        UnionTypeDefinitionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var description = RewriteNodeOrDefault(node.Description, context);
        var directives = RewriteList(node.Directives, context);
        var types = RewriteList(node.Types, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(description, node.Description) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(types, node.Types))
        {
            return new UnionTypeDefinitionNode(
                node.Location,
                name,
                description,
                directives,
                types);
        }

        return node;
    }

    protected virtual UnionTypeExtensionNode? RewriteUnionTypeExtension(
        UnionTypeExtensionNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);
        var directives = RewriteList(node.Directives, context);
        var types = RewriteList(node.Types, context);

        if (!ReferenceEquals(name, node.Name) ||
            !ReferenceEquals(directives, node.Directives) ||
            !ReferenceEquals(types, node.Types))
        {
            return new UnionTypeExtensionNode(
                node.Location,
                name,
                directives,
                types);
        }

        return node;
    }

    protected virtual VariableDefinitionNode? RewriteVariableDefinition(
        VariableDefinitionNode node,
        TContext context)
    {
        var variable = RewriteNode(node.Variable, context);
        var type = RewriteNode(node.Type, context);
        var defaultValue = RewriteNodeOrDefault(node.DefaultValue, context);
        var directives = RewriteList(node.Directives, context);

        if (!ReferenceEquals(variable, node.Variable) ||
            !ReferenceEquals(type, node.Type) ||
            !ReferenceEquals(defaultValue, node.DefaultValue) ||
            !ReferenceEquals(directives, node.Directives))
        {
            return new VariableDefinitionNode(
                node.Location,
                variable,
                type,
                defaultValue,
                directives);
        }

        return node;
    }

    protected virtual VariableNode? RewriteVariable(
        VariableNode node,
        TContext context)
    {
        var name = RewriteNode(node.Name, context);

        if (!ReferenceEquals(name, node.Name))
        {
            return new VariableNode(node.Location, name);
        }

        return node;
    }

    protected T RewriteNode<T>(T node, TContext context) where T : ISyntaxNode
        => (T?)Rewrite(node, context) ?? throw new SyntaxNodeCannotBeNullException(node);

    protected T? RewriteNodeOrDefault<T>(T? node, TContext context) where T : ISyntaxNode
        => node is null ? default : (T?)Rewrite(node, context);

    protected IReadOnlyList<T> RewriteList<T>(IReadOnlyList<T> nodes, TContext context)
        where T : ISyntaxNode
    {
        T?[]? rewrittenList = null;

        var includedNodes = 0;
        for (var i = 0; i < nodes.Count; i++)
        {
            var originalNode = nodes[i];
            var rewrittenNode = RewriteNodeOrDefault(originalNode, context);

            if (rewrittenList is null)
            {
                if (ReferenceEquals(originalNode, rewrittenNode))
                {
                    continue;
                }

                rewrittenList = new T[nodes.Count];

                for (var j = 0; j < i; j++)
                {
                    rewrittenList[includedNodes] = nodes[j];
                    includedNodes++;
                }
            }

            if (rewrittenNode is null)
            {
                continue;
            }

            rewrittenList[includedNodes] = rewrittenNode;
            includedNodes++;
        }

        if (rewrittenList is null)
        {
            return nodes;
        }

        Array.Resize(ref rewrittenList, includedNodes);
        return rewrittenList!;
    }
}
