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
    private readonly ConcurrentDictionary<ulong, RewrittenOperation> _cache = new();
    private readonly Dictionary<string, LookupFieldInfo> _lookupFields;

    /// <summary>
    /// Initializes a new instance of <see cref="FederationQueryRewriter"/>.
    /// </summary>
    /// <param name="lookupFields">
    /// A dictionary mapping query field names (e.g. <c>"productById"</c>) to their
    /// <see cref="LookupFieldInfo"/> describing the entity type and key argument mappings.
    /// </param>
    public FederationQueryRewriter(Dictionary<string, LookupFieldInfo> lookupFields)
    {
        ArgumentNullException.ThrowIfNull(lookupFields);
        _lookupFields = lookupFields;
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
            return RewriteEntityLookup(operationDefinition, lookupField, lookupInfo);
        }

        // Not an entity lookup — pass through unchanged.
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
        OperationDefinitionNode operationDefinition,
        FieldNode lookupField,
        LookupFieldInfo lookupInfo)
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

        // 2. Build the _entities query AST.
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
            selectionSet: lookupField.SelectionSet
                ?? new SelectionSetNode(Array.Empty<ISelectionNode>()));

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
