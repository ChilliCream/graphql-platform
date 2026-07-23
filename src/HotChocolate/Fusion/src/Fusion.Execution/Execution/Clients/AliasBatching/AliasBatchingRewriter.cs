using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Merges one or more source schema requests into a single alias batched GraphQL
/// operation. Each row of every inbound request becomes an aliased copy of that
/// operation's root selections, every variable reference is renamed per row, and all
/// variable definitions are merged into one cross product so the batch executes as a
/// single spec conformant GraphQL request.
/// </summary>
/// <remarks>
/// The rewriter is stateless and may be shared across requests. It does not extract
/// fragments. Each row's selection set is inlined.
/// </remarks>
internal sealed class AliasBatchingRewriter
{
    private static readonly VariableRenameRewriter s_variableRenamer = new();

    /// <summary>
    /// Merges the given requests into a single alias batched operation.
    /// </summary>
    /// <param name="requests">
    /// The inbound requests to merge. A request with an empty variable set counts as one row.
    /// </param>
    /// <returns>
    /// The merged operation, its document hash, and the prefix table mapping aliases and
    /// prefixed variable names back to the inbound requests and rows.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a request targets a subscription, when a multi-operation merge contains a
    /// mutation, or when an inbound variable name starts with an underscore followed by a digit
    /// (which would collide with a prefixed name after rewriting).
    /// </exception>
    public AliasBatchedOperation Rewrite(ImmutableArray<SourceSchemaClientRequest> requests)
    {
        if (requests.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException(
                "Alias batching requires at least one request.");
        }

        var operationCount = requests.Length;
        var operations = new OperationDefinitionNode[operationCount];
        var rowsPerOperation = new int[operationCount];
        var hasMutation = false;

        for (var op = 0; op < operationCount; op++)
        {
            var request = requests[op];

            if (request.OperationType == OperationType.Subscription)
            {
                throw new InvalidOperationException(
                    "Alias batching is not supported for subscriptions.");
            }

            if (request.OperationType == OperationType.Mutation)
            {
                hasMutation = true;
            }

            operations[op] = ParseOperation(request.OperationSourceText);
            rowsPerOperation[op] = Math.Max(request.Variables.Length, 1);
        }

        if (operationCount > 1 && hasMutation)
        {
            throw new InvalidOperationException(
                "Alias batching cannot merge multiple operations when any of them is a mutation.");
        }

        var selections = new List<ISelectionNode>();
        var variableDefinitions = new List<VariableDefinitionNode>();

        var rootAliases = new List<string>();
        var rootOperationIndices = new List<int>();
        var rootRowIndices = new List<int>();
        var rootResponseNames = new List<string>();

        var prefixedVariableNames = new List<string>();
        var originalVariableNames = new List<string>();
        var variableOperationIndices = new List<int>();
        var variableRowIndices = new List<int>();

        for (var op = 0; op < operationCount; op++)
        {
            var operation = operations[op];
            var rootFields = CollectRootFields(operation);
            var hasSingleRoot = rootFields.Count == 1;
            var rowCount = rowsPerOperation[op];

            for (var row = 0; row < rowCount; row++)
            {
                var variablePrefix = BuildVariablePrefix(operationCount, rowCount, op, row);
                var renameMap = BuildRenameMap(
                    operation.VariableDefinitions,
                    variablePrefix,
                    op,
                    row,
                    prefixedVariableNames,
                    originalVariableNames,
                    variableOperationIndices,
                    variableRowIndices);

                foreach (var rootField in rootFields)
                {
                    var responseName = rootField.Alias?.Value ?? rootField.Name.Value;
                    var alias = BuildRootAlias(
                        operationCount,
                        rowCount,
                        hasSingleRoot,
                        op,
                        row,
                        responseName);

                    var rewrittenField = RewriteRootField(rootField, alias, renameMap);
                    selections.Add(rewrittenField);

                    rootAliases.Add(alias);
                    rootOperationIndices.Add(op);
                    rootRowIndices.Add(row);
                    rootResponseNames.Add(responseName);
                }

                AppendVariableDefinitions(
                    operation.VariableDefinitions,
                    renameMap,
                    variableDefinitions);
            }
        }

        var mergedOperation = BuildMergedOperation(
            operations,
            operationCount,
            hasMutation,
            variableDefinitions,
            selections);

        var sourceText = mergedOperation.ToString(indented: true);

        var prefixes = new AliasPrefixTable
        {
            VariableOperationIndices = AsImmutable(variableOperationIndices),
            VariableRowIndices = AsImmutable(variableRowIndices),
            OriginalVariableNames = AsImmutable(originalVariableNames),
            PrefixedVariableNames = AsImmutable(prefixedVariableNames),
            RootOperationIndices = AsImmutable(rootOperationIndices),
            RootRowIndices = AsImmutable(rootRowIndices),
            RootAliases = AsImmutable(rootAliases)
        };

        return new AliasBatchedOperation
        {
            SourceText = sourceText,
            Prefixes = prefixes,
            RootResponseNames = AsImmutable(rootResponseNames)
        };
    }

    private static ImmutableArray<T> AsImmutable<T>(List<T> source)
        => ImmutableCollectionsMarshal.AsImmutableArray(source.ToArray());

    private static OperationDefinitionNode ParseOperation(string operationSourceText)
    {
        var document = Utf8GraphQLParser.Parse(operationSourceText);

        foreach (var definition in document.Definitions)
        {
            if (definition is OperationDefinitionNode operation)
            {
                return operation;
            }
        }

        throw new InvalidOperationException(
            "The request does not contain a GraphQL operation definition.");
    }

    private static List<FieldNode> CollectRootFields(OperationDefinitionNode operation)
    {
        var rootFields = new List<FieldNode>(operation.SelectionSet.Selections.Count);

        for (var i = 0; i < operation.SelectionSet.Selections.Count; i++)
        {
            if (operation.SelectionSet.Selections[i] is FieldNode field)
            {
                rootFields.Add(field);
            }
            else
            {
                throw new InvalidOperationException(
                    "Alias batching supports only field selections at the operation root. "
                    + "Inline fragments and fragment spreads at the root are not supported.");
            }
        }

        if (rootFields.Count == 0)
        {
            throw new InvalidOperationException(
                "Alias batching requires at least one root field per operation.");
        }

        return rootFields;
    }

    private static string BuildVariablePrefix(
        int operationCount,
        int rowCount,
        int op,
        int row)
    {
        if (operationCount == 1)
        {
            // Single operation: $_{row}{originalName}.
            return $"_{row}";
        }

        if (rowCount == 1)
        {
            // Multi operation, single row: $_{op}{originalName}.
            return $"_{op}";
        }

        // Multi operation, multiple rows: $_{op}_{row}{originalName}.
        return $"_{op}_{row}";
    }

    private static string BuildRootAlias(
        int operationCount,
        int rowCount,
        bool hasSingleRoot,
        int op,
        int row,
        string responseName)
    {
        // Operations with several root fields always carry the operation and row
        // index plus the response name so the aliased copies for one row do not
        // collide. Single root operations use the simpler shape from the spec.
        if (!hasSingleRoot)
        {
            return $"_{op}_{row}_{responseName}";
        }

        if (operationCount == 1)
        {
            // Single operation, single root: _{row}.
            return $"_{row}";
        }

        if (rowCount == 1)
        {
            // Multi operation, single row, single root: _{op}.
            return $"_{op}";
        }

        // Multi operation, multiple rows, single root: _{op}_{row}.
        return $"_{op}_{row}";
    }

    private static Dictionary<string, string> BuildRenameMap(
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        string variablePrefix,
        int op,
        int row,
        List<string> prefixedVariableNames,
        List<string> originalVariableNames,
        List<int> variableOperationIndices,
        List<int> variableRowIndices)
    {
        var renameMap = new Dictionary<string, string>(
            variableDefinitions.Count,
            StringComparer.Ordinal);

        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var originalName = variableDefinitions[i].Variable.Name.Value;
            EnsureNoPrefixCollision(originalName);

            var prefixedName = variablePrefix + originalName;
            renameMap[originalName] = prefixedName;

            prefixedVariableNames.Add(prefixedName);
            originalVariableNames.Add(originalName);
            variableOperationIndices.Add(op);
            variableRowIndices.Add(row);
        }

        return renameMap;
    }

    private static void EnsureNoPrefixCollision(string variableName)
    {
        if (variableName.Length >= 2
            && variableName[0] == '_'
            && char.IsAsciiDigit(variableName[1]))
        {
            throw new InvalidOperationException(
                $"The variable '${variableName}' cannot be alias batched because its name starts "
                + "with an underscore followed by a digit, which collides with the alias batching "
                + "prefix scheme.");
        }
    }

    private static FieldNode RewriteRootField(
        FieldNode rootField,
        string alias,
        Dictionary<string, string> renameMap)
    {
        var aliasedField = new FieldNode(
            null,
            rootField.Name,
            new NameNode(alias),
            rootField.Directives,
            rootField.Arguments,
            rootField.SelectionSet);

        if (renameMap.Count == 0)
        {
            return aliasedField;
        }

        return (FieldNode?)s_variableRenamer.Rewrite(aliasedField, renameMap) ?? aliasedField;
    }

    private static void AppendVariableDefinitions(
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        Dictionary<string, string> renameMap,
        List<VariableDefinitionNode> target)
    {
        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var definition = variableDefinitions[i];
            var prefixedName = renameMap[definition.Variable.Name.Value];

            target.Add(new VariableDefinitionNode(
                null,
                new VariableNode(prefixedName),
                definition.Description,
                definition.Type,
                definition.DefaultValue,
                RenameDirectives(definition.Directives, renameMap)));
        }
    }

    private static IReadOnlyList<DirectiveNode> RenameDirectives(
        IReadOnlyList<DirectiveNode> directives,
        Dictionary<string, string> renameMap)
    {
        // Variable-definition directives are run through the same renamer that rewrites the
        // field tree, so any variable reference is prefixed with the row's scheme rather than
        // being left untouched. Const directives pass through unchanged.
        if (directives.Count == 0 || renameMap.Count == 0)
        {
            return directives;
        }

        var renamed = new DirectiveNode[directives.Count];
        var changed = false;

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];
            var rewritten = (DirectiveNode?)s_variableRenamer.Rewrite(directive, renameMap) ?? directive;
            renamed[i] = rewritten;

            if (!ReferenceEquals(rewritten, directive))
            {
                changed = true;
            }
        }

        return changed ? renamed : directives;
    }

    private static OperationDefinitionNode BuildMergedOperation(
        OperationDefinitionNode[] operations,
        int operationCount,
        bool hasMutation,
        List<VariableDefinitionNode> variableDefinitions,
        List<ISelectionNode> selections)
    {
        var template = operations[0];

        // A single-operation mutation keeps its mutation semantics. A multi-operation
        // merge is always a query (mutations are rejected earlier).
        var operationType = operationCount == 1 && hasMutation
            ? OperationType.Mutation
            : OperationType.Query;

        var directives = ResolveOperationDirectives(operations, operationCount);

        return new OperationDefinitionNode(
            null,
            template.Name,
            description: null,
            operationType,
            variableDefinitions,
            directives,
            new SelectionSetNode(selections));
    }

    private static IReadOnlyList<DirectiveNode> ResolveOperationDirectives(
        OperationDefinitionNode[] operations,
        int operationCount)
    {
        // A single inbound operation carries its operation-level directives through to the
        // merged operation unchanged. Merging the operation-level directives of several
        // operations is undefined, so the merge is rejected when any of them declares one.
        if (operationCount == 1)
        {
            return operations[0].Directives;
        }

        for (var op = 0; op < operationCount; op++)
        {
            if (operations[op].Directives.Count > 0)
            {
                throw new InvalidOperationException(
                    "Alias batching cannot merge multiple operations when any of them declares "
                    + "operation-level directives, because merging operation-level directives "
                    + "across operations is undefined.");
            }
        }

        return [];
    }

    private sealed class VariableRenameRewriter : SyntaxRewriter<Dictionary<string, string>>
    {
        protected override VariableNode? RewriteVariable(
            VariableNode node,
            Dictionary<string, string> context)
        {
            if (context.TryGetValue(node.Name.Value, out var renamed))
            {
                return new VariableNode(node.Location, new NameNode(renamed));
            }

            return node;
        }

        protected override DirectiveNode? RewriteDirective(
            DirectiveNode node,
            Dictionary<string, string> context)
        {
            // The base rewriter does not descend into directive arguments, so we
            // rewrite them here to catch variables such as @include(if: $x).
            var name = RewriteNode(node.Name, context);
            var arguments = RewriteList(node.Arguments, context);

            if (!ReferenceEquals(name, node.Name)
                || !ReferenceEquals(arguments, node.Arguments))
            {
                return new DirectiveNode(node.Location, name, arguments);
            }

            return node;
        }
    }
}
