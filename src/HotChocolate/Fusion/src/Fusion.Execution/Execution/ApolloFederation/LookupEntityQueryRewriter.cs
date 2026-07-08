using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.ApolloFederation;

/// <summary>
/// Rewrites a Fusion planner lookup query into an Apollo Federation
/// <c>_entities</c> query.
/// <para>
/// The Fusion planner emits a single root lookup field (e.g.
/// <c>productById(id: $__fusion_1_id)</c>). This rewriter resolves the lookup
/// against the composite schema, lifts the lookup field's selection set into an
/// <c>... on &lt;EntityType&gt;</c> inline fragment, and strips the key and
/// <c>@require</c> arguments whose values move into the representation objects at
/// execution time.
/// </para>
/// </summary>
internal static class LookupEntityQueryRewriter
{
    /// <summary>
    /// Rewrites a lookup operation into an Apollo Federation <c>_entities</c> query.
    /// </summary>
    /// <param name="schema">The composite schema definition.</param>
    /// <param name="schemaName">The name of the Apollo Federation source schema.</param>
    /// <param name="operation">The planner lookup operation.</param>
    /// <returns>
    /// The rewritten operation together with the entity type name and the original
    /// root lookup field.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operation does not contain a root lookup field or no lookup
    /// matches the root field in the given source schema.
    /// </exception>
    public static RewrittenOperation Rewrite(
        FusionSchemaDefinition schema,
        string schemaName,
        OperationSourceText operation)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        var document = Utf8GraphQLParser.Parse(operation.SourceText);
        var operationDefinition = GetOperationDefinition(document);
        var lookupField = GetLookupField(operationDefinition);
        var lookup = ResolveLookup(schema, schemaName, lookupField);
        var entityTypeName = lookup.FieldType.Name;
        var entityType = schema.Types.GetType<FusionObjectTypeDefinition>(entityTypeName);

        // Lift the lookup field's selection set into the inline fragment, stripping
        // the variable-bound '@require' arguments at any nesting depth.
        var strippedSelections = StripRequireArguments(
            schema,
            schemaName,
            entityType,
            lookupField.SelectionSet);

        var text = BuildEntitiesDocument(
            entityTypeName,
            strippedSelections,
            operationDefinition.VariableDefinitions);

        return new RewrittenOperation(
            new OperationSourceText(operation.Name, operation.Type, text, operation.Hash),
            entityTypeName,
            lookupField);
    }

    private static FieldNode GetLookupField(OperationDefinitionNode operationDefinition)
    {
        var selections = operationDefinition.SelectionSet.Selections;

        if (selections.Count == 0 || selections[0] is not FieldNode lookupField)
        {
            throw new InvalidOperationException(
                "The lookup operation does not contain a root lookup field.");
        }

        return lookupField;
    }

    private static Lookup ResolveLookup(
        FusionSchemaDefinition schema,
        string schemaName,
        FieldNode lookupField)
    {
        Lookup? exactMatch = null;
        Lookup? fallback = null;

        foreach (var type in schema.Types.AsEnumerable(allowInaccessibleFields: true))
        {
            if (type is not FusionObjectTypeDefinition objectType
                || !objectType.IsEntityType
                || !objectType.Sources.TryGetMember(schemaName, out var sourceObjectType))
            {
                continue;
            }

            foreach (var lookup in sourceObjectType.Lookups)
            {
                if (!string.Equals(lookup.FieldName, lookupField.Name.Value, StringComparison.Ordinal))
                {
                    continue;
                }

                if (ArgumentNamesMatch(lookup, lookupField))
                {
                    // Prefer a public lookup whose argument set matches the root field.
                    if (!lookup.IsInternal)
                    {
                        return lookup;
                    }

                    exactMatch ??= lookup;
                }

                fallback ??= lookup;
            }
        }

        var resolved = exactMatch ?? fallback;

        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"No lookup matching the root field '{lookupField.Name.Value}' was found "
                + $"in the source schema '{schemaName}'.");
        }

        return resolved;
    }

    private static bool ArgumentNamesMatch(Lookup lookup, FieldNode lookupField)
    {
        if (lookup.Arguments.Length != lookupField.Arguments.Count)
        {
            return false;
        }

        foreach (var argument in lookupField.Arguments)
        {
            var found = false;

            foreach (var lookupArgument in lookup.Arguments)
            {
                if (string.Equals(lookupArgument.Name, argument.Name.Value, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the selection set with every variable-bound <c>@require</c> argument
    /// removed at any nesting depth.
    /// </summary>
    private static SelectionSetNode StripRequireArguments(
        FusionSchemaDefinition schema,
        string schemaName,
        FusionComplexTypeDefinition? type,
        SelectionSetNode? selectionSet)
    {
        if (selectionSet is null)
        {
            return new SelectionSetNode([]);
        }

        var selections = selectionSet.Selections;
        List<ISelectionNode>? rewritten = null;

        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];

            if (selection is InlineFragmentNode inlineFragment)
            {
                var rewrittenFragment = StripInlineFragment(
                    schema,
                    schemaName,
                    type,
                    inlineFragment);

                if (rewrittenFragment is null)
                {
                    rewritten?.Add(selection);
                    continue;
                }

                rewritten ??= CopyUpTo(selections, i);
                rewritten.Add(rewrittenFragment);
                continue;
            }

            if (selection is not FieldNode fieldNode
                || type is null
                || !type.Fields.TryGetField(
                    fieldNode.Name.Value,
                    allowInaccessibleFields: true,
                    out var field))
            {
                rewritten?.Add(selection);
                continue;
            }

            var retained = StripFieldArguments(schemaName, field, fieldNode);
            var rewrittenChild = StripChildSelections(schema, schemaName, field, fieldNode);

            if (retained is null && rewrittenChild is null)
            {
                rewritten?.Add(selection);
                continue;
            }

            rewritten ??= CopyUpTo(selections, i);

            var newField = fieldNode;

            if (retained is not null)
            {
                newField = newField.WithArguments(retained);
            }

            if (rewrittenChild is not null)
            {
                newField = newField.WithSelectionSet(rewrittenChild);
            }

            rewritten.Add(newField);
        }

        return rewritten is null ? selectionSet : new SelectionSetNode(rewritten);
    }

    /// <summary>
    /// Returns the retained arguments of a field with the variable-bound
    /// <c>@require</c> arguments removed, or <c>null</c> when nothing was stripped.
    /// </summary>
    private static List<ArgumentNode>? StripFieldArguments(
        string schemaName,
        FusionOutputFieldDefinition field,
        FieldNode fieldNode)
    {
        var arguments = fieldNode.Arguments;

        if (arguments.Count == 0)
        {
            return null;
        }

        var requireArguments = GetRequireArguments(schemaName, field);

        if (requireArguments is null)
        {
            return null;
        }

        List<ArgumentNode>? retained = null;

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];

            if (argument.Value is VariableNode
                && requireArguments.Contains(argument.Name.Value))
            {
                retained ??= CopyUpTo(arguments, i);
                continue;
            }

            retained?.Add(argument);
        }

        return retained;
    }

    /// <summary>
    /// Recurses into an inline fragment so that <c>@require</c> arguments inside it
    /// are stripped too. The fragment's type condition supplies the type context of
    /// its selections. Returns <c>null</c> when the fragment is unchanged.
    /// </summary>
    private static InlineFragmentNode? StripInlineFragment(
        FusionSchemaDefinition schema,
        string schemaName,
        FusionComplexTypeDefinition? type,
        InlineFragmentNode inlineFragment)
    {
        var fragmentType = type;

        if (inlineFragment.TypeCondition is { } typeCondition
            && schema.Types.TryGetType<FusionComplexTypeDefinition>(
                typeCondition.Name.Value,
                allowInaccessibleFields: true,
                out var conditionType))
        {
            fragmentType = conditionType;
        }

        var rewrittenSelections = StripRequireArguments(
            schema,
            schemaName,
            fragmentType,
            inlineFragment.SelectionSet);

        return ReferenceEquals(rewrittenSelections, inlineFragment.SelectionSet)
            ? null
            : inlineFragment.WithSelectionSet(rewrittenSelections);
    }

    /// <summary>
    /// Recurses into a field's selection set so that nested <c>@require</c> arguments
    /// are stripped too. Returns <c>null</c> when the child selection set is unchanged.
    /// </summary>
    private static SelectionSetNode? StripChildSelections(
        FusionSchemaDefinition schema,
        string schemaName,
        FusionOutputFieldDefinition field,
        FieldNode fieldNode)
    {
        if (fieldNode.SelectionSet is not { } childSelectionSet)
        {
            return null;
        }

        // The child type is unresolved for union types; the walk still descends
        // because inline fragments within supply their own type contexts.
        schema.Types.TryGetType<FusionComplexTypeDefinition>(
            field.Type.NamedType().Name,
            allowInaccessibleFields: true,
            out var childType);

        var rewrittenChild = StripRequireArguments(
            schema,
            schemaName,
            childType,
            childSelectionSet);

        return ReferenceEquals(rewrittenChild, childSelectionSet) ? null : rewrittenChild;
    }

    /// <summary>
    /// Returns the names of the field's arguments that carry a <c>@require</c>
    /// selection in the given source schema, or <c>null</c> when the field has none.
    /// </summary>
    private static HashSet<string>? GetRequireArguments(
        string schemaName,
        FusionOutputFieldDefinition field)
    {
        if (!field.Sources.TryGetMember(schemaName, out var sourceField)
            || sourceField.Requirements is not { } requirements)
        {
            return null;
        }

        HashSet<string>? requireArguments = null;

        for (var i = 0; i < requirements.Arguments.Length; i++)
        {
            if (requirements.Fields[i] is null)
            {
                continue;
            }

            requireArguments ??= [with(StringComparer.Ordinal)];
            requireArguments.Add(requirements.Arguments[i].Name);
        }

        return requireArguments;
    }

    private static string BuildEntitiesDocument(
        string entityTypeName,
        SelectionSetNode selectionSet,
        IReadOnlyList<VariableDefinitionNode> availableVariableDefinitions)
    {
        var representationsVarDef = new VariableDefinitionNode(
            location: null,
            new VariableNode("representations"),
            description: null,
            type: new NonNullTypeNode(
                new ListTypeNode(
                    new NonNullTypeNode(
                        new NamedTypeNode("_Any")))),
            defaultValue: null,
            directives: []);

        var inlineFragment = new InlineFragmentNode(
            location: null,
            typeCondition: new NamedTypeNode(entityTypeName),
            directives: [],
            selectionSet: selectionSet);

        var entitiesField = new FieldNode(
            location: null,
            new NameNode("_entities"),
            alias: null,
            directives: [],
            arguments: [new ArgumentNode("representations", new VariableNode("representations"))],
            selectionSet: new SelectionSetNode([inlineFragment]));

        var operationDefinition = new OperationDefinitionNode(
            location: null,
            name: null,
            description: null,
            operation: OperationType.Query,
            variableDefinitions: BuildVariableDefinitions(
                selectionSet,
                availableVariableDefinitions,
                representationsVarDef),
            directives: [],
            selectionSet: new SelectionSetNode([entitiesField]));

        return new DocumentNode([operationDefinition]).ToString(indented: true);
    }

    private static List<VariableDefinitionNode> BuildVariableDefinitions(
        SelectionSetNode selectionSet,
        IReadOnlyList<VariableDefinitionNode> availableVariableDefinitions,
        VariableDefinitionNode representationsVarDef)
    {
        if (availableVariableDefinitions.Count == 0)
        {
            return [representationsVarDef];
        }

        var usedVariables = new HashSet<string>(StringComparer.Ordinal);
        CollectVariables(selectionSet, usedVariables);

        if (usedVariables.Count == 0)
        {
            return [representationsVarDef];
        }

        var variableDefinitions = new List<VariableDefinitionNode> { representationsVarDef };

        foreach (var variableDefinition in availableVariableDefinitions)
        {
            if (usedVariables.Contains(variableDefinition.Variable.Name.Value))
            {
                variableDefinitions.Add(variableDefinition);
            }
        }

        return variableDefinitions;
    }

    private static void CollectVariables(ISyntaxNode node, HashSet<string> variables)
    {
        if (node is VariableNode variable)
        {
            variables.Add(variable.Name.Value);
            return;
        }

        foreach (var child in node.GetNodes())
        {
            CollectVariables(child, variables);
        }
    }

    private static OperationDefinitionNode GetOperationDefinition(DocumentNode document)
    {
        for (var i = 0; i < document.Definitions.Count; i++)
        {
            if (document.Definitions[i] is OperationDefinitionNode operation)
            {
                return operation;
            }
        }

        throw new InvalidOperationException("The document does not contain an operation definition.");
    }

    private static List<T> CopyUpTo<T>(IReadOnlyList<T> source, int count)
    {
        var list = new List<T>(source.Count);

        for (var i = 0; i < count; i++)
        {
            list.Add(source[i]);
        }

        return list;
    }
}
