using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Satisfiability;

internal sealed class SatisfiabilityFactsBuilder
{
    private readonly MutableSchemaDefinition _schema;
    private readonly FusionLookupDirectiveCache _lookupCache;
    private readonly HashSet<ReachablePositionKey> _reachablePositions = [];
    private readonly HashSet<SatisfiabilityFacts.FieldResolvableFactKey> _fieldResolvableFacts = [];
    private readonly HashSet<SatisfiabilityFacts.CanTransitionFactKey> _canTransitionFacts = [];
    private readonly HashSet<SatisfiabilityFacts.FieldAccessibleFactKey> _fieldAccessibleFacts = [];
    private readonly HashSet<ExpandedFieldAccessKey> _expandedFieldAccesses = [];
    private readonly Dictionary<IDirective, SelectionSetNode> _lookupKeySelectionSets = [];
    private readonly Dictionary<MutableObjectTypeDefinition, string[]> _candidateTargetsByType = [];

    /// <summary>
    /// Initializes a new instance of <see cref="SatisfiabilityFactsBuilder"/>.
    /// </summary>
    /// <param name="schema">The merged schema whose satisfiability facts are computed.</param>
    /// <param name="lookupCache">The lookup directive cache for the merged schema.</param>
    public SatisfiabilityFactsBuilder(
        MutableSchemaDefinition schema,
        FusionLookupDirectiveCache lookupCache)
    {
        _schema = schema;
        _lookupCache = lookupCache;
    }

    /// <summary>
    /// Builds the least fixpoint satisfiability fact table.
    /// </summary>
    /// <returns>The computed satisfiability facts.</returns>
    public SatisfiabilityFacts Build()
    {
        SeedRootPositions();

        var changed = true;

        while (changed)
        {
            changed = false;
            var positions = _reachablePositions.ToArray();
            var reachedTypes = new HashSet<MutableObjectTypeDefinition>();

            foreach (var position in positions)
            {
                if (reachedTypes.Add(position.Type))
                {
                    changed |= DeriveResolvableFields(position.Type);
                }
            }

            foreach (var position in positions)
            {
                changed |= DeriveTransitions(position.Type, position.FromSchema);
            }

            foreach (var position in positions)
            {
                changed |= DeriveAccessibleFields(position.Type, position.FromSchema);
            }
        }

        return new SatisfiabilityFacts(_canTransitionFacts, _fieldAccessibleFacts, _fieldResolvableFacts);
    }

    private bool DeriveResolvableFields(MutableObjectTypeDefinition type)
    {
        var changed = false;

        foreach (var field in type.Fields)
        {
            if (field.HasFusionInaccessibleDirective())
            {
                continue;
            }

            foreach (var schemaName in field.GetSchemaNames())
            {
                if (field.IsPartial(schemaName))
                {
                    continue;
                }

                var requirements = field.GetFusionRequiresRequirements(schemaName);

                if (requirements is not null
                    && !CanResolveSelectionSet(
                        requirements,
                        type,
                        fromSchema: schemaName,
                        excludeSchema: schemaName,
                        SelectionSetResolutionMode.FieldRequire))
                {
                    continue;
                }

                changed |= _fieldResolvableFacts.Add(
                    new SatisfiabilityFacts.FieldResolvableFactKey(type, field, schemaName));
            }
        }

        return changed;
    }

    private bool DeriveTransitions(MutableObjectTypeDefinition type, string fromSchema)
    {
        var changed = false;

        // Only the schemas that declare a lookup for the type are transition targets; scanning those
        // instead of every source schema per round is the corpus-scale win. The per-target lookup
        // call keeps the schema-specific union-membership filtering.
        foreach (var targetSchema in GetCandidateTargetSchemas(type))
        {
            if (targetSchema == fromSchema
                || _canTransitionFacts.Contains(
                    new SatisfiabilityFacts.CanTransitionFactKey(type, targetSchema, fromSchema)))
            {
                continue;
            }

            foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type, targetSchema))
            {
                var keySelectionSet = GetLookupKeySelectionSet(lookup);

                if (!CanResolveSelectionSet(
                    keySelectionSet,
                    type,
                    fromSchema,
                    excludeSchema: targetSchema,
                    SelectionSetResolutionMode.LookupKey))
                {
                    continue;
                }

                if (_canTransitionFacts.Add(
                    new SatisfiabilityFacts.CanTransitionFactKey(type, targetSchema, fromSchema)))
                {
                    changed = true;
                    changed |= _reachablePositions.Add(new ReachablePositionKey(type, targetSchema));
                }

                break;
            }
        }

        return changed;
    }

    private string[] GetCandidateTargetSchemas(MutableObjectTypeDefinition type)
    {
        if (!_candidateTargetsByType.TryGetValue(type, out var targets))
        {
            var set = new HashSet<string>(StringComparer.Ordinal);

            foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type))
            {
                set.Add((string)lookup.Arguments[WellKnownArgumentNames.Schema].Value!);
            }

            targets = [.. set];
            _candidateTargetsByType.Add(type, targets);
        }

        return targets;
    }

    private bool DeriveAccessibleFields(MutableObjectTypeDefinition type, string fromSchema)
    {
        var changed = false;

        foreach (var field in type.Fields)
        {
            if (field.HasFusionInaccessibleDirective())
            {
                continue;
            }

            foreach (var schemaName in field.GetSchemaNames())
            {
                if (!IsFieldAccessibleVia(type, field, schemaName, fromSchema))
                {
                    continue;
                }

                changed |= _fieldAccessibleFacts.Add(
                    new SatisfiabilityFacts.FieldAccessibleFactKey(type, field, fromSchema));
                changed |= AddReachableChildPositions(type, field, schemaName, fromSchema);
            }
        }

        return changed;
    }

    private bool IsFieldAccessibleVia(
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        string sourceSchema,
        string fromSchema)
    {
        return _fieldResolvableFacts.Contains(
                new SatisfiabilityFacts.FieldResolvableFactKey(type, field, sourceSchema))
            && (sourceSchema == fromSchema
                || _canTransitionFacts.Contains(
                    new SatisfiabilityFacts.CanTransitionFactKey(type, sourceSchema, fromSchema)));
    }

    private bool AddReachableChildPositions(
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        string schemaName,
        string fromSchema)
    {
        if (!_expandedFieldAccesses.Add(new ExpandedFieldAccessKey(type, field, schemaName, fromSchema)))
        {
            return false;
        }

        var fieldType = field.Type.AsTypeDefinition();

        if (fieldType.Kind is not TypeKind.Object and not TypeKind.Interface and not TypeKind.Union)
        {
            return false;
        }

        var changed = false;

        foreach (var possibleType in fieldType.GetPossibleTypes(schemaName, _schema))
        {
            changed |= _reachablePositions.Add(new ReachablePositionKey(possibleType, schemaName));
        }

        return changed;
    }

    private bool CanResolveSelectionSet(
        SelectionSetNode selectionSet,
        MutableObjectTypeDefinition contextType,
        string fromSchema,
        string excludeSchema,
        SelectionSetResolutionMode mode)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (!CanResolveSelection(selection, contextType, fromSchema, excludeSchema, mode))
            {
                return false;
            }
        }

        return true;
    }

    private bool CanResolveSelection(
        ISelectionNode selection,
        MutableObjectTypeDefinition contextType,
        string fromSchema,
        string excludeSchema,
        SelectionSetResolutionMode mode)
    {
        switch (selection)
        {
            case FieldNode fieldNode:
                return CanResolveFieldSelection(fieldNode, contextType, fromSchema, excludeSchema, mode);

            case InlineFragmentNode { TypeCondition: null } inlineFragmentNode:
                return CanResolveSelectionSet(
                    inlineFragmentNode.SelectionSet,
                    contextType,
                    fromSchema,
                    excludeSchema,
                    mode);

            case InlineFragmentNode inlineFragmentNode:
                var fragmentType = _schema.Types[inlineFragmentNode.TypeCondition.Name.Value];

                foreach (var possibleType in _schema.GetPossibleTypes(fragmentType))
                {
                    if (!CanResolveSelectionSet(
                        inlineFragmentNode.SelectionSet,
                        possibleType,
                        fromSchema,
                        excludeSchema,
                        mode))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return true;
        }
    }

    private bool CanResolveFieldSelection(
        FieldNode fieldNode,
        MutableObjectTypeDefinition contextType,
        string fromSchema,
        string excludeSchema,
        SelectionSetResolutionMode mode)
    {
        if (!contextType.Fields.TryGetField(fieldNode.Name.Value, out var field))
        {
            return false;
        }

        foreach (var schemaName in GetCandidateSchemaNames(field, excludeSchema, fieldNode, mode))
        {
            if (!IsFieldAccessibleVia(contextType, field, schemaName, fromSchema))
            {
                continue;
            }

            if (fieldNode.SelectionSet is null)
            {
                return true;
            }

            if (CanResolveChildSelectionSet(
                field,
                schemaName,
                fieldNode.SelectionSet,
                excludeSchema,
                mode))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanResolveChildSelectionSet(
        MutableOutputFieldDefinition field,
        string schemaName,
        SelectionSetNode selectionSet,
        string excludeSchema,
        SelectionSetResolutionMode mode)
    {
        var fieldType = field.Type.AsTypeDefinition();

        if (fieldType.Kind is not TypeKind.Object and not TypeKind.Interface and not TypeKind.Union)
        {
            return false;
        }

        foreach (var possibleType in fieldType.GetPossibleTypes(schemaName, _schema))
        {
            if (!CanResolveSelectionSet(selectionSet, possibleType, schemaName, excludeSchema, mode))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> GetCandidateSchemaNames(
        MutableOutputFieldDefinition field,
        string excludeSchema,
        FieldNode fieldNode,
        SelectionSetResolutionMode mode)
    {
        var excludeCandidate =
            fieldNode.SelectionSet is null || mode == SelectionSetResolutionMode.LookupKey;

        foreach (var schemaName in field.GetSchemaNames())
        {
            if (excludeCandidate && schemaName == excludeSchema)
            {
                continue;
            }

            yield return schemaName;
        }
    }

    private SelectionSetNode GetLookupKeySelectionSet(IDirective lookup)
    {
        if (!_lookupKeySelectionSets.TryGetValue(lookup, out var selectionSet))
        {
            var lookupKey = (string)lookup.Arguments[WellKnownArgumentNames.Key].Value!;
            selectionSet = ParseSelectionSet($"{{ {lookupKey} }}");
            _lookupKeySelectionSets.Add(lookup, selectionSet);
        }

        return selectionSet;
    }

    private void SeedRootPositions()
    {
        SeedRootPosition(_schema.QueryType);
        SeedRootPosition(_schema.MutationType);
        SeedRootPosition(_schema.SubscriptionType);
    }

    private void SeedRootPosition(MutableObjectTypeDefinition? rootType)
    {
        if (rootType is null)
        {
            return;
        }

        foreach (var schemaName in GetTypeSchemaNames(rootType))
        {
            _reachablePositions.Add(new ReachablePositionKey(rootType, schemaName));
        }

        foreach (var field in rootType.Fields)
        {
            foreach (var schemaName in field.GetSchemaNames())
            {
                _reachablePositions.Add(new ReachablePositionKey(rootType, schemaName));
            }
        }
    }

    private static IEnumerable<string> GetTypeSchemaNames(IDirectivesProvider type)
    {
        foreach (var directive in type.Directives.AsEnumerable())
        {
            if (directive.Name == WellKnownDirectiveNames.FusionType)
            {
                yield return (string)directive.Arguments[WellKnownArgumentNames.Schema].Value!;
            }
        }
    }

    private enum SelectionSetResolutionMode
    {
        FieldRequire,
        LookupKey
    }

    private readonly record struct ReachablePositionKey(
        MutableObjectTypeDefinition Type,
        string FromSchema);

    private readonly record struct ExpandedFieldAccessKey(
        MutableObjectTypeDefinition Type,
        MutableOutputFieldDefinition Field,
        string SchemaName,
        string FromSchema);
}
