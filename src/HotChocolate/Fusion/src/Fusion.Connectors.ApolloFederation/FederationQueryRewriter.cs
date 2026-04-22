using System.Collections.Concurrent;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Rewrites Fusion planner queries into Apollo Federation <c>_entities</c> queries.
/// <para>
/// The Fusion planner emits queries against lookup fields (e.g. <c>productById(id: $__fusion_1_id)</c>).
/// This rewriter detects those lookup fields, extracts the variable-to-key-field mapping,
/// and produces an <c>_entities(representations: $representations)</c> query with the
/// appropriate inline fragment.
/// </para>
/// <para>
/// Non-lookup fields are passed through unchanged.
/// </para>
/// </summary>
internal sealed class FederationQueryRewriter
{
    private static readonly IReadOnlyDictionary<string, EntityRequiresInfo> s_emptyEntityRequires
        = new Dictionary<string, EntityRequiresInfo>(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<ulong, RewrittenOperation> _cache = new();
    private readonly IReadOnlyDictionary<string, LookupFieldInfo> _lookupFields;
    private readonly IReadOnlyDictionary<string, EntityRequiresInfo> _entityRequires;

    /// <summary>
    /// Initializes a new instance of <see cref="FederationQueryRewriter"/>.
    /// </summary>
    /// <param name="lookupFields">
    /// A dictionary mapping query field names (e.g. <c>"productById"</c>) to their
    /// <see cref="LookupFieldInfo"/> describing the entity type and key argument mappings.
    /// </param>
    /// <param name="entityRequires">
    /// A dictionary keyed by entity type name (e.g. <c>"Product"</c>) that
    /// describes the <c>@require</c> arguments declared on each entity field.
    /// Optional; when <see langword="null"/> the rewriter treats every entity
    /// type as having no require arguments.
    /// </param>
    public FederationQueryRewriter(
        IReadOnlyDictionary<string, LookupFieldInfo> lookupFields,
        IReadOnlyDictionary<string, EntityRequiresInfo>? entityRequires = null)
    {
        ArgumentNullException.ThrowIfNull(lookupFields);
        _lookupFields = lookupFields;
        _entityRequires = entityRequires ?? s_emptyEntityRequires;
    }

    /// <summary>
    /// Returns a cached rewritten operation for the given hash, or rewrites the
    /// operation source text and caches the result.
    /// </summary>
    /// <param name="operationSourceText">The GraphQL operation text from the Fusion planner.</param>
    /// <param name="operationHash">A precomputed hash used as the cache key.</param>
    /// <returns>The rewritten operation.</returns>
    public RewrittenOperation GetOrRewrite(string operationSourceText, ulong operationHash)
    {
        return _cache.GetOrAdd(operationHash, _ => Rewrite(operationSourceText));
    }

    private RewrittenOperation Rewrite(string operationSourceText)
    {
        var document = Utf8GraphQLParser.Parse(operationSourceText);

        var operationDefinition = GetOperationDefinition(document);
        var selections = operationDefinition.SelectionSet.Selections;

        // Check if the first top-level field is a lookup field.
        if (selections.Count > 0
            && selections[0] is FieldNode lookupField
            && _lookupFields.TryGetValue(lookupField.Name.Value, out var lookupInfo))
        {
            return RewriteEntityLookup(lookupField, lookupInfo, _entityRequires);
        }

        // Not an entity lookup, pass through unchanged.
        return new RewrittenOperation
        {
            OperationText = operationSourceText,
            IsEntityLookup = false,
            EntityTypeName = null,
            VariableToKeyFieldMap = new Dictionary<string, string>(),
            LookupFieldName = null
        };
    }

    private static RewrittenOperation RewriteEntityLookup(
        FieldNode lookupField,
        LookupFieldInfo lookupInfo,
        IReadOnlyDictionary<string, EntityRequiresInfo> entityRequires)
    {
        // 1. Build the variable-to-key-field mapping by inspecting the lookup field's arguments.
        //    The planner passes arguments like: productById(id: $__fusion_1_id)
        //    We map variable name "__fusion_1_id" → key field "id".
        var variableToKeyFieldMap = new Dictionary<string, string>();

        foreach (var argument in lookupField.Arguments)
        {
            if (argument.Value is VariableNode variable
                && lookupInfo.ArgumentToKeyFieldMap.TryGetValue(argument.Name.Value, out var keyFieldName))
            {
                variableToKeyFieldMap[variable.Name.Value] = keyFieldName;
            }
        }

        // 2. Project any '@require' arguments declared on the lookup field's
        //    selection into the representation. The planner emits the require
        //    arguments as a variable reference on each inline-fragment field;
        //    we strip those arguments from the outgoing selection and record
        //    the variable-to-representation mapping so the client can splice
        //    the bound variable value onto the representation body.
        //
        //    The walk only descends into the lookup field's top-level selection
        //    set. Nested '@require' paths (require arguments on fields nested
        //    inside other selections) are not yet handled; the composer does
        //    not currently generate them for any enabled compliance suite.
        var innerSelections = StripRequireArguments(
            lookupField.SelectionSet,
            lookupInfo.EntityTypeName,
            entityRequires,
            variableToKeyFieldMap);

        // 3. Build the _entities query AST.
        //    query($representations: [_Any!]!) {
        //      _entities(representations: $representations) {
        //        ... on EntityType { <inner selections> }
        //      }
        //    }

        // The $representations variable definition: $representations: [_Any!]!
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

        // The inline fragment: ... on Product { id name price }
        var inlineFragment = new InlineFragmentNode(
            location: null,
            typeCondition: new NamedTypeNode(lookupInfo.EntityTypeName),
            directives: [],
            selectionSet: innerSelections);

        // The _entities field: _entities(representations: $representations) { ... on Product { ... } }
        var entitiesField = new FieldNode(
            location: null,
            new NameNode("_entities"),
            alias: null,
            directives: [],
            arguments: [new ArgumentNode("representations", new VariableNode("representations"))],
            selectionSet: new SelectionSetNode([inlineFragment]));

        // The operation: query($representations: [_Any!]!) { _entities(...) { ... } }
        var rewrittenOperation = new OperationDefinitionNode(
            location: null,
            name: null,
            description: null,
            operation: OperationType.Query,
            variableDefinitions: [representationsVarDef],
            directives: [],
            selectionSet: new SelectionSetNode([entitiesField]));

        var rewrittenDocument = new DocumentNode([rewrittenOperation]);

        return new RewrittenOperation
        {
            OperationText = rewrittenDocument.ToString(indented: true),
            IsEntityLookup = true,
            EntityTypeName = lookupInfo.EntityTypeName,
            VariableToKeyFieldMap = variableToKeyFieldMap,
            LookupFieldName = lookupField.Name.Value,
            InlineFragment = inlineFragment
        };
    }

    /// <summary>
    /// Returns the lookup field's inner selection set with every
    /// <c>@require</c>-tagged argument removed. The stripped variables are
    /// recorded into <paramref name="variableToKeyFieldMap"/> so that the
    /// client merges them into the <c>_entities</c> representation body.
    /// </summary>
    private static SelectionSetNode StripRequireArguments(
        SelectionSetNode? selectionSet,
        string entityTypeName,
        IReadOnlyDictionary<string, EntityRequiresInfo> entityRequires,
        Dictionary<string, string> variableToKeyFieldMap)
    {
        if (selectionSet is null)
        {
            return new SelectionSetNode(Array.Empty<ISelectionNode>());
        }

        if (!entityRequires.TryGetValue(entityTypeName, out var requiresInfo)
            || requiresInfo.Fields.Count == 0)
        {
            return selectionSet;
        }

        var selections = selectionSet.Selections;
        List<ISelectionNode>? rewritten = null;

        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];

            if (selection is not FieldNode fieldNode
                || !requiresInfo.Fields.TryGetValue(fieldNode.Name.Value, out var requiresArgs))
            {
                rewritten?.Add(selection);
                continue;
            }

            // Walk the field's arguments and drop any that match a require
            // argument name; record the bound variable name against the
            // require field path so the client can inject it into the
            // representation.
            var arguments = fieldNode.Arguments;
            List<ArgumentNode>? retained = null;

            for (var j = 0; j < arguments.Count; j++)
            {
                var argument = arguments[j];

                if (requiresArgs.TryGetValue(argument.Name.Value, out var requireFieldPath)
                    && argument.Value is VariableNode variable)
                {
                    variableToKeyFieldMap[variable.Name.Value] = requireFieldPath;

                    if (retained is null)
                    {
                        retained = new List<ArgumentNode>(arguments.Count);
                        for (var k = 0; k < j; k++)
                        {
                            retained.Add(arguments[k]);
                        }
                    }

                    continue;
                }

                retained?.Add(argument);
            }

            if (retained is null)
            {
                rewritten?.Add(selection);
                continue;
            }

            if (rewritten is null)
            {
                rewritten = new List<ISelectionNode>(selections.Count);
                for (var k = 0; k < i; k++)
                {
                    rewritten.Add(selections[k]);
                }
            }

            rewritten.Add(fieldNode.WithArguments(retained));
        }

        if (rewritten is null)
        {
            return selectionSet;
        }

        return new SelectionSetNode(rewritten);
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
}
