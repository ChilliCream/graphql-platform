using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.ApolloFederation;

/// <summary>
/// Compiles a lookup field and its operation requirements into the representation
/// shape used to build representations for <c>_entities</c> fetches.
/// <para>
/// An argument binds a requirement when its value is a variable named after a
/// requirement key. Maps bound on the root lookup field anchor at the representation
/// root; maps bound on nested fields anchor at the selection set containing the field.
/// </para>
/// </summary>
internal static class RepresentationShapeBuilder
{
    public static ImmutableArray<RepresentationShapeNode> Build(
        OperationSourceText operation,
        string entityTypeName,
        SelectionPath target,
        ReadOnlySpan<OperationRequirement> requiredData,
        ResultSelectionSet resultSelectionSet,
        FusionSchemaDefinition schema)
    {
        var bindings = CreateBindings(
            operation,
            entityTypeName,
            target,
            requiredData,
            resultSelectionSet);

        return Build(bindings, requiredData, schema, entityTypeName);
    }

    internal static ImmutableArray<RepresentationBinding> CreateBindings(
        OperationSourceText operation,
        string entityTypeName,
        SelectionPath target,
        ReadOnlySpan<OperationRequirement> requiredData,
        ResultSelectionSet resultSelectionSet)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityTypeName);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(resultSelectionSet);

        var document = Utf8GraphQLParser.Parse(operation.Value.Span);
        var entitySelectionSet = GetEntitySelectionSet(document, entityTypeName);
        var bindings = ImmutableArray.CreateBuilder<RepresentationBinding>(requiredData.Length);

        for (var i = 0; i < requiredData.Length; i++)
        {
            var requirement = requiredData[i];
            EnsureTargetPrefix(target, requirement);

            var path = ImmutableArray.CreateBuilder<RepresentationPathSegment>(
                requirement.Path.Length - target.Length);
            var operationLevel = entitySelectionSet;
            var resultLevel = resultSelectionSet;

            for (var p = target.Length; p < requirement.Path.Length; p++)
            {
                var segment = requirement.Path[p];

                if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
                {
                    if (TryGetInlineFragment(operationLevel, segment.Name, out var fragmentLevel))
                    {
                        operationLevel = fragmentLevel;
                    }

                    resultLevel = resultLevel.TryGetFragment(segment.Name)
                        ?? throw new InvalidOperationException(
                            $"The result selection has no fragment for type '{segment.Name}'.");
                    continue;
                }

                var responseName = segment.Name;
                var sourceResponseName = resultLevel.TryMapResponseName(responseName, out var mapping)
                    ? mapping.SourceResponseName
                    : responseName;
                var field = GetField(operationLevel, sourceResponseName);

                if (mapping.FieldName is not null
                    && !string.Equals(mapping.FieldName, field.Name.Value, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The result selection maps '{responseName}' to the source field "
                        + $"'{mapping.FieldName}', but the rewritten Apollo operation selects "
                        + $"'{field.Name.Value}'.");
                }

                path.Add(new RepresentationPathSegment(field.Name.Value, responseName));

                if (p + 1 < requirement.Path.Length)
                {
                    operationLevel = field.SelectionSet
                        ?? throw new InvalidOperationException(
                            $"The rewritten Apollo operation field '{sourceResponseName}' has no "
                            + "selection set for a nested representation requirement.");
                    resultLevel = resultLevel.TryGetChild(responseName)
                        ?? throw new InvalidOperationException(
                            $"The result selection has no child selection set for '{responseName}'.");
                }
            }

            bindings.Add(new RepresentationBinding(requirement.Key, path.ToImmutable()));
        }

        return bindings.MoveToImmutable();
    }

    /// <summary>
    /// Extracts the requirement bindings of a lookup field.
    /// </summary>
    /// <param name="lookupField">The original, un-stripped root lookup field.</param>
    /// <param name="requiredData">The operation requirements of the lookup.</param>
    /// <returns>
    /// One binding per operation requirement, in the order the lookup selection binds them.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a requirement is not bound by exactly one argument of the
    /// lookup selection.
    /// </exception>
    public static ImmutableArray<RepresentationBinding> CreateBindings(
        FieldNode lookupField,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        ArgumentNullException.ThrowIfNull(lookupField);

        var bindings = new List<RepresentationBinding>(requiredData.Length);
        var path = new List<RepresentationPathSegment>();
        var matched = requiredData.Length <= 64
            ? stackalloc bool[requiredData.Length]
            : new bool[requiredData.Length];
        matched.Clear();

        AddRequirementBindings(lookupField, path, bindings, requiredData, matched);
        WalkBindings(lookupField.SelectionSet, path, bindings, requiredData, matched);
        EnsureAllRequirementsAreBound(requiredData, matched);

        return [.. bindings];
    }

    /// <summary>
    /// Builds the representation shape for a lookup field.
    /// </summary>
    /// <param name="lookupField">The original, un-stripped root lookup field.</param>
    /// <param name="requiredData">The operation requirements of the lookup.</param>
    /// <param name="schema">
    /// The composite schema used to resolve the declared types along the
    /// requirement paths, which detects abstract composite positions that must
    /// carry a runtime <c>__typename</c> in the representation.
    /// </param>
    /// <param name="entityTypeName">
    /// The name of the entity type the representation is built for. It is the
    /// root type from which the declared requirement-path types are resolved.
    /// </param>
    /// <returns>
    /// The root level of the representation shape. The result is a plan-time
    /// immutable constant that callers may cache and reuse across concurrent
    /// executions.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a requirement is not bound by exactly one argument of the
    /// lookup selection, or when requirement maps produce conflicting nodes.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when a requirement map uses a value selection construct that
    /// cannot be represented in a representation object.
    /// </exception>
    public static ImmutableArray<RepresentationShapeNode> Build(
        FieldNode lookupField,
        ReadOnlySpan<OperationRequirement> requiredData,
        FusionSchemaDefinition schema,
        string entityTypeName)
        => Build(CreateBindings(lookupField, requiredData), requiredData, schema, entityTypeName);

    /// <summary>
    /// Builds the representation shape from the requirement bindings of a lookup.
    /// </summary>
    /// <param name="bindings">The requirement bindings of the lookup.</param>
    /// <param name="requiredData">The operation requirements of the lookup.</param>
    /// <param name="schema">
    /// The composite schema used to resolve the declared types along the
    /// requirement paths, which detects abstract composite positions that must
    /// carry a runtime <c>__typename</c> in the representation.
    /// </param>
    /// <param name="entityTypeName">
    /// The name of the entity type the representation is built for. It is the
    /// root type from which the declared requirement-path types are resolved.
    /// </param>
    /// <returns>
    /// The root level of the representation shape. The result is a plan-time
    /// immutable constant that callers may cache and reuse across concurrent
    /// executions.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a binding does not resolve to exactly one requirement, when a
    /// requirement is unbound, or when requirement maps produce conflicting nodes.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when a requirement map uses a value selection construct that
    /// cannot be represented in a representation object.
    /// </exception>
    public static ImmutableArray<RepresentationShapeNode> Build(
        ImmutableArray<RepresentationBinding> bindings,
        ReadOnlySpan<OperationRequirement> requiredData,
        FusionSchemaDefinition schema,
        string entityTypeName)
    {
        var root = new List<MutableRepresentationShapeNode>();
        var matched = requiredData.Length <= 64
            ? stackalloc bool[requiredData.Length]
            : new bool[requiredData.Length];
        matched.Clear();

        foreach (var binding in bindings)
        {
            var index = GetRequirementIndex(requiredData, binding.RequirementKey);

            if (index < 0)
            {
                throw new InvalidOperationException(
                    "The representation binding references the requirement "
                    + $"'{binding.RequirementKey}', which the lookup does not declare.");
            }

            if (matched[index])
            {
                throw new InvalidOperationException(
                    $"The lookup selection binds the requirement '{requiredData[index].Key}' "
                    + "to more than one argument.");
            }

            matched[index] = true;
            var level = root;

            foreach (var segment in binding.Path)
            {
                level = GetOrCreateStructuralNode(level, segment);
            }

            AddValueSelection(
                level,
                requiredData[index].Map,
                index,
                [],
                requiredData[index].Type,
                requiredData[index].InternalAlias);
        }

        EnsureAllRequirementsAreBound(requiredData, matched);

        // Bake the abstract-composite decision now, at plan build, so the
        // emitter's per-entity write path reads a single flag instead of
        // resolving types per value.
        if (schema.Types.TryGetType<FusionComplexTypeDefinition>(
            entityTypeName,
            allowInaccessibleFields: true,
            out var entityType))
        {
            AnnotateAbstractComposites(root, entityType, schema);
        }

        return Freeze(root);
    }

    private static SelectionSetNode GetEntitySelectionSet(
        DocumentNode document,
        string entityTypeName)
    {
        OperationDefinitionNode? operation = null;

        foreach (var definition in document.Definitions)
        {
            if (definition is OperationDefinitionNode operationDefinition)
            {
                operation = operationDefinition;
                break;
            }
        }

        if (operation is null)
        {
            throw new InvalidOperationException(
                "The rewritten Apollo operation does not contain an operation definition.");
        }

        FieldNode? entitiesField = null;

        foreach (var selection in operation.SelectionSet.Selections)
        {
            if (selection is FieldNode field
                && string.Equals(field.Name.Value, "_entities", StringComparison.Ordinal))
            {
                entitiesField = field;
                break;
            }
        }

        if (entitiesField?.SelectionSet is not { } entitiesSelectionSet)
        {
            throw new InvalidOperationException(
                "The rewritten Apollo operation does not contain an _entities selection.");
        }

        foreach (var selection in entitiesSelectionSet.Selections)
        {
            if (selection is InlineFragmentNode
                {
                    TypeCondition: { } typeCondition
                } inlineFragment
                && string.Equals(
                    typeCondition.Name.Value,
                    entityTypeName,
                    StringComparison.Ordinal))
            {
                return inlineFragment.SelectionSet;
            }
        }

        throw new InvalidOperationException(
            $"The rewritten Apollo operation does not select the entity type '{entityTypeName}'.");
    }

    private static void EnsureTargetPrefix(
        SelectionPath target,
        OperationRequirement requirement)
    {
        if (requirement.Path.Length < target.Length)
        {
            throw new InvalidOperationException(
                $"The representation requirement '{requirement.Key}' is outside the Apollo "
                + "operation target.");
        }

        for (var i = 0; i < target.Length; i++)
        {
            if (!target[i].Equals(requirement.Path[i]))
            {
                throw new InvalidOperationException(
                    $"The representation requirement '{requirement.Key}' is outside the Apollo "
                    + "operation target.");
            }
        }
    }

    private static bool TryGetInlineFragment(
        SelectionSetNode selectionSet,
        string typeName,
        out SelectionSetNode fragmentSelectionSet)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is InlineFragmentNode inlineFragment)
            {
                if (string.Equals(
                    inlineFragment.TypeCondition?.Name.Value,
                    typeName,
                    StringComparison.Ordinal))
                {
                    fragmentSelectionSet = inlineFragment.SelectionSet;
                    return true;
                }

                if (inlineFragment.TypeCondition is null
                    && TryGetInlineFragment(
                        inlineFragment.SelectionSet,
                        typeName,
                        out fragmentSelectionSet))
                {
                    return true;
                }
            }
        }

        fragmentSelectionSet = null!;
        return false;
    }

    private static FieldNode GetField(
        SelectionSetNode selectionSet,
        string responseName)
    {
        FieldNode? match = null;
        FindFields(selectionSet, responseName, ref match);

        return match
            ?? throw new InvalidOperationException(
                "The rewritten Apollo operation does not select the response field "
                + $"'{responseName}'.");
    }

    private static void FindFields(
        SelectionSetNode selectionSet,
        string responseName,
        ref FieldNode? match)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field
                    when string.Equals(
                        field.Alias?.Value ?? field.Name.Value,
                        responseName,
                        StringComparison.Ordinal):
                    if (match is not null
                        && !string.Equals(match.Name.Value, field.Name.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "The rewritten Apollo operation resolves the response field "
                            + $"'{responseName}' from more than one source field.");
                    }

                    match ??= field;
                    break;

                case InlineFragmentNode { TypeCondition: null } inlineFragment:
                    FindFields(inlineFragment.SelectionSet, responseName, ref match);
                    break;
            }
        }
    }

    private static void EnsureAllRequirementsAreBound(
        ReadOnlySpan<OperationRequirement> requiredData,
        ReadOnlySpan<bool> matched)
    {
        for (var i = 0; i < requiredData.Length; i++)
        {
            if (!matched[i])
            {
                throw new InvalidOperationException(
                    $"The lookup selection does not bind the requirement '{requiredData[i].Key}' "
                    + "to an argument.");
            }
        }
    }

    // Walks the built shape against the declared types, flagging every
    // non-branched composite node whose declared type is abstract so the
    // emitter writes its runtime __typename. Branched nodes already carry an
    // unconditional __typename via their branch handling and are left unflagged.
    private static void AnnotateAbstractComposites(
        List<MutableRepresentationShapeNode> level,
        FusionComplexTypeDefinition declaringType,
        FusionSchemaDefinition schema)
    {
        for (var i = 0; i < level.Count; i++)
        {
            var node = level[i];

            if (node.Children is null || node.IsList)
            {
                continue;
            }

            var fieldDeclaringType = declaringType;

            if (node.ParentTypeCondition is { } parentTypeCondition
                && schema.Types.TryGetType<FusionComplexTypeDefinition>(
                    parentTypeCondition,
                    allowInaccessibleFields: true,
                    out var conditionedType))
            {
                fieldDeclaringType = conditionedType;
            }

            if (!fieldDeclaringType.Fields.TryGetField(
                node.Name,
                allowInaccessibleFields: true,
                out var field))
            {
                continue;
            }

            var namedType = field.Type.NamedType();

            if (node.Branches is not { Count: > 0 } && namedType.IsAbstractType())
            {
                node.RequiresTypeName = true;
            }

            if (namedType is not FusionComplexTypeDefinition complexFieldType)
            {
                continue;
            }

            AnnotateAbstractComposites(node.Children, complexFieldType, schema);

            if (node.Branches is { } branches)
            {
                for (var b = 0; b < branches.Count; b++)
                {
                    if (schema.Types.TryGetType<FusionComplexTypeDefinition>(
                        branches[b].TypeCondition,
                        allowInaccessibleFields: true,
                        out var branchType))
                    {
                        AnnotateAbstractComposites(branches[b].Children, branchType, schema);
                    }
                }
            }
        }
    }

    private static void WalkBindings(
        SelectionSetNode? selectionSet,
        List<RepresentationPathSegment> path,
        List<RepresentationBinding> bindings,
        ReadOnlySpan<OperationRequirement> requiredData,
        Span<bool> matched)
    {
        if (selectionSet is null)
        {
            return;
        }

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            switch (selectionSet.Selections[i])
            {
                case FieldNode field:
                    AddRequirementBindings(field, path, bindings, requiredData, matched);

                    if (HasRequirementArguments(field.SelectionSet, requiredData))
                    {
                        path.Add(new RepresentationPathSegment(
                            field.Name.Value,
                            field.Alias?.Value ?? field.Name.Value));
                        WalkBindings(field.SelectionSet, path, bindings, requiredData, matched);
                        path.RemoveAt(path.Count - 1);
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    // An inline fragment adds no level to the result data, so its
                    // selections contribute to the current level.
                    WalkBindings(inlineFragment.SelectionSet, path, bindings, requiredData, matched);
                    break;
            }
        }
    }

    private static void AddRequirementBindings(
        FieldNode field,
        List<RepresentationPathSegment> path,
        List<RepresentationBinding> bindings,
        ReadOnlySpan<OperationRequirement> requiredData,
        Span<bool> matched)
    {
        for (var i = 0; i < field.Arguments.Count; i++)
        {
            if (field.Arguments[i].Value is not VariableNode variable)
            {
                continue;
            }

            var index = GetRequirementIndex(requiredData, variable.Name.Value);

            if (index < 0)
            {
                continue;
            }

            if (matched[index])
            {
                throw new InvalidOperationException(
                    $"The lookup selection binds the requirement '{requiredData[index].Key}' "
                    + "to more than one argument.");
            }

            matched[index] = true;
            bindings.Add(new RepresentationBinding(requiredData[index].Key, [.. path]));
        }
    }

    private static int GetRequirementIndex(
        ReadOnlySpan<OperationRequirement> requiredData,
        string variableName)
    {
        for (var i = 0; i < requiredData.Length; i++)
        {
            if (string.Equals(requiredData[i].Key, variableName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool HasRequirementArguments(
        SelectionSetNode? selectionSet,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        if (selectionSet is null)
        {
            return false;
        }

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            switch (selectionSet.Selections[i])
            {
                case FieldNode field:
                    for (var j = 0; j < field.Arguments.Count; j++)
                    {
                        if (field.Arguments[j].Value is VariableNode variable
                            && GetRequirementIndex(requiredData, variable.Name.Value) >= 0)
                        {
                            return true;
                        }
                    }

                    if (HasRequirementArguments(field.SelectionSet, requiredData))
                    {
                        return true;
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    if (HasRequirementArguments(inlineFragment.SelectionSet, requiredData))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static void AddValueSelection(
        List<MutableRepresentationShapeNode> level,
        IValueSelectionNode selection,
        int requirementIndex,
        List<string> lhsPath,
        ITypeNode? inputType,
        string? rootResponseName)
    {
        switch (selection)
        {
            case PathNode path:
                AddPath(level, path, requirementIndex, lhsPath, inputType, rootResponseName);
                break;

            case ObjectValueSelectionNode objectValue:
                AddObjectFields(level, objectValue, requirementIndex, lhsPath, rootResponseName);
                break;

            case PathObjectValueSelectionNode pathObject:
                var terminal = EnsureCompositeChain(level, pathObject.Path, rootResponseName);
                AddObjectFields(
                    terminal,
                    pathObject.ObjectValueSelection,
                    requirementIndex,
                    lhsPath,
                    rootResponseName: null);
                break;

            case PathListValueSelectionNode pathList:
                AddPathList(level, pathList, requirementIndex, lhsPath, inputType, rootResponseName);
                break;

            default:
                throw new NotSupportedException(
                    $"The value selection '{selection.GetType().Name}' cannot be projected "
                    + "into an entity representation.");
        }
    }

    private static void AddPath(
        List<MutableRepresentationShapeNode> level,
        PathNode path,
        int requirementIndex,
        List<string> lhsPath,
        ITypeNode? inputType,
        string? rootResponseName)
    {
        var currentLevel = level;
        var parentTypeCondition = path.TypeName?.Value;
        var segment = path.PathSegment;
        var responseName = rootResponseName ?? segment.FieldName.Value;

        while (segment.PathSegment is not null)
        {
            // A null intermediate of a plain path resolves the leaf value to
            // null, so the node must not make the entity unresolvable.
            currentLevel = GetOrCreateCompositeNode(
                currentLevel,
                segment.FieldName.Value,
                responseName,
                parentTypeCondition,
                segment.TypeName?.Value,
                skipOnNull: false);
            parentTypeCondition = null;
            segment = segment.PathSegment;
            responseName = segment.FieldName.Value;
        }

        AddLeafNode(
            currentLevel,
            segment.FieldName.Value,
            responseName,
            requirementIndex,
            lhsPath,
            parentTypeCondition,
            segment.TypeName?.Value,
            inputType);
    }

    private static void AddPathList(
        List<MutableRepresentationShapeNode> level,
        PathListValueSelectionNode pathList,
        int requirementIndex,
        List<string> lhsPath,
        ITypeNode? inputType,
        string? rootResponseName)
    {
        if (pathList.ListValueSelection.ElementSelection
            is not ObjectValueSelectionNode elementSelection)
        {
            throw new NotSupportedException(
                "Only object value selections are supported as list elements of a "
                + "requirement map.");
        }

        var currentLevel = level;
        var parentTypeCondition = pathList.Path.TypeName?.Value;
        var segment = pathList.Path.PathSegment;
        var responseName = rootResponseName ?? segment.FieldName.Value;

        while (segment.PathSegment is not null)
        {
            // A null intermediate of a list path resolves the list value to
            // null, so the node must not make the entity unresolvable.
            currentLevel = GetOrCreateCompositeNode(
                currentLevel,
                segment.FieldName.Value,
                responseName,
                parentTypeCondition,
                segment.TypeName?.Value,
                skipOnNull: false);
            parentTypeCondition = null;
            segment = segment.PathSegment;
            responseName = segment.FieldName.Value;
        }

        var elementType = GetElementType(inputType);
        var existing = FindNode(currentLevel, segment.FieldName.Value);

        if (existing is not null)
        {
            if (!existing.IsList
                || !string.Equals(existing.ResponseName, responseName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The requirement maps produce conflicting representation nodes "
                    + $"for '{segment.FieldName.Value}'.");
            }

            // A list value is supplied as a whole by its first requirement, but
            // a null value must skip the entity when any requirement's input
            // position cannot be satisfied by null.
            existing.SkipOnNull |= IsNonNullPosition(inputType);
            existing.ElementInputType = MergeElementInputType(existing.ElementInputType, elementType);
            return;
        }

        var listNode = CreateNode(segment.FieldName.Value, responseName);
        listNode.Children = [];
        listNode.IsList = true;
        listNode.RequirementIndex = requirementIndex;
        listNode.LhsPath = [.. lhsPath];
        listNode.ParentTypeCondition = parentTypeCondition;
        listNode.TypeCondition = segment.TypeName?.Value;
        listNode.SkipOnNull = IsNonNullPosition(inputType);
        listNode.ElementInputType = elementType;
        currentLevel.Add(listNode);

        // Element fields resolve against a single list element, so the input
        // path restarts at the list boundary.
        AddObjectFields(listNode.Children, elementSelection, requirementIndex, [], rootResponseName: null);
    }

    private static void AddObjectFields(
        List<MutableRepresentationShapeNode> level,
        ObjectValueSelectionNode objectValue,
        int requirementIndex,
        List<string> lhsPath,
        string? rootResponseName)
    {
        foreach (var field in objectValue.Fields)
        {
            lhsPath.Add(field.Name.Value);
            var fieldRootResponseName =
                rootResponseName is not null && objectValue.Fields.Length == 1
                    ? rootResponseName
                    : null;

            if (field.ValueSelection is null)
            {
                AddLeafNode(
                    level,
                    field.Name.Value,
                    fieldRootResponseName ?? field.Name.Value,
                    requirementIndex,
                    lhsPath,
                    parentTypeCondition: null,
                    typeCondition: null,
                    inputType: null);
            }
            else
            {
                AddValueSelection(
                    level,
                    field.ValueSelection,
                    requirementIndex,
                    lhsPath,
                    inputType: null,
                    rootResponseName: fieldRootResponseName);
            }

            lhsPath.RemoveAt(lhsPath.Count - 1);
        }
    }

    private static List<MutableRepresentationShapeNode> EnsureCompositeChain(
        List<MutableRepresentationShapeNode> level,
        PathNode path,
        string? rootResponseName)
    {
        var currentLevel = level;
        var parentTypeCondition = path.TypeName?.Value;
        var segment = path.PathSegment;
        var responseName = rootResponseName ?? segment.FieldName.Value;

        while (true)
        {
            // An object value selection is unresolvable when any segment of
            // its path is null, so the whole chain skips the entity on null.
            currentLevel = GetOrCreateCompositeNode(
                currentLevel,
                segment.FieldName.Value,
                responseName,
                parentTypeCondition,
                segment.TypeName?.Value,
                skipOnNull: true);

            if (segment.PathSegment is null)
            {
                return currentLevel;
            }

            parentTypeCondition = null;
            segment = segment.PathSegment;
            responseName = segment.FieldName.Value;
        }
    }

    // A structural node always emits an object. A list-valued structural field is
    // not representable here, but the planner re-roots nested requirements at
    // depth 1, so list-shaped structural parents do not occur; the builder is
    // type-blind and cannot guard against them.
    private static List<MutableRepresentationShapeNode> GetOrCreateStructuralNode(
        List<MutableRepresentationShapeNode> level,
        RepresentationPathSegment segment)
    {
        var name = segment.Name;
        var responseName = segment.ResponseName;
        var existing = FindNode(level, name);

        if (existing is not null)
        {
            if (existing.Children is null || existing.IsList)
            {
                throw new InvalidOperationException(
                    $"The requirement maps produce conflicting representation nodes for '{name}'.");
            }

            if (!string.Equals(existing.ResponseName, responseName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The lookup selection resolves the representation node '{name}' "
                    + "under conflicting response names.");
            }

            // Requirements lifted from below a structural field are unresolvable
            // when the field is null, so skip-on-null wins for merged nodes.
            existing.SkipOnNull = true;
            return existing.Children;
        }

        var node = CreateNode(name, responseName);
        node.Children = [];
        node.SkipOnNull = true;
        level.Add(node);
        return node.Children;
    }

    private static List<MutableRepresentationShapeNode> GetOrCreateCompositeNode(
        List<MutableRepresentationShapeNode> level,
        string name,
        string responseName,
        string? parentTypeCondition,
        string? typeCondition,
        bool skipOnNull)
    {
        var existing = FindNode(level, name);

        if (existing is not null)
        {
            if (existing.Children is null || existing.IsList)
            {
                throw new InvalidOperationException(
                    $"The requirement maps produce conflicting representation nodes for '{name}'.");
            }

            if (!string.Equals(existing.ResponseName, responseName, StringComparison.Ordinal)
                || !string.Equals(existing.ParentTypeCondition, parentTypeCondition, StringComparison.Ordinal)
                || existing.TypeCondition is not null)
            {
                throw new InvalidOperationException(
                    $"The requirement maps produce conflicting representation nodes for '{name}'.");
            }

            // When requirements of both null behaviors merge under one node,
            // skip-on-null wins: the skipping requirement is unresolvable on null.
            existing.SkipOnNull |= skipOnNull;

            if (typeCondition is null)
            {
                return existing.Children;
            }

            return GetOrCreateBranch(existing, typeCondition).Children;
        }

        var node = CreateNode(name, responseName);
        node.Children = [];
        node.ParentTypeCondition = parentTypeCondition;
        node.SkipOnNull = skipOnNull;
        level.Add(node);

        if (typeCondition is null)
        {
            return node.Children;
        }

        return GetOrCreateBranch(node, typeCondition).Children;
    }

    private static MutableRepresentationShapeBranch GetOrCreateBranch(
        MutableRepresentationShapeNode node,
        string typeCondition)
    {
        if (node.Branches is { } branches)
        {
            for (var i = 0; i < branches.Count; i++)
            {
                if (string.Equals(branches[i].TypeCondition, typeCondition, StringComparison.Ordinal))
                {
                    return branches[i];
                }
            }
        }
        else
        {
            branches = [];
            node.Branches = branches;
        }

        var branch = new MutableRepresentationShapeBranch { TypeCondition = typeCondition };
        branches.Add(branch);
        return branch;
    }

    private static void AddLeafNode(
        List<MutableRepresentationShapeNode> level,
        string name,
        string responseName,
        int requirementIndex,
        List<string> lhsPath,
        string? parentTypeCondition,
        string? typeCondition,
        ITypeNode? inputType)
    {
        var skipOnNull = IsNonNullPosition(inputType);
        var elementInputType = GetElementType(inputType);
        var existing = FindNode(level, name);

        if (existing is not null)
        {
            if (existing.Children is not null
                || !string.Equals(existing.ResponseName, responseName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The requirement maps produce conflicting representation nodes for '{name}'.");
            }

            // A leaf value is supplied by its first requirement, but a null
            // value must skip the entity when any requirement's input position
            // cannot be satisfied by null.
            existing.SkipOnNull |= skipOnNull;
            existing.ElementInputType = MergeElementInputType(existing.ElementInputType, elementInputType);
            return;
        }

        var node = CreateNode(name, responseName);
        node.RequirementIndex = requirementIndex;
        node.LhsPath = [.. lhsPath];
        node.ParentTypeCondition = parentTypeCondition;
        node.TypeCondition = typeCondition;
        node.SkipOnNull = skipOnNull;
        node.ElementInputType = elementInputType;
        level.Add(node);
    }

    private static MutableRepresentationShapeNode CreateNode(string name, string responseName)
        => new()
        {
            Name = name,
            ResponseName = responseName
        };

    private static MutableRepresentationShapeNode? FindNode(
        List<MutableRepresentationShapeNode> level,
        string name)
    {
        for (var i = 0; i < level.Count; i++)
        {
            if (string.Equals(level[i].Name, name, StringComparison.Ordinal))
            {
                return level[i];
            }
        }

        return null;
    }

    private static ImmutableArray<RepresentationShapeNode> Freeze(
        List<MutableRepresentationShapeNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return ImmutableArray<RepresentationShapeNode>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<RepresentationShapeNode>(nodes.Count);

        foreach (var node in nodes)
        {
            builder.Add(
                new RepresentationShapeNode(
                    node.Name,
                    node.ResponseName,
                    node.Children is null ? default : Freeze(node.Children),
                    FreezeBranches(node.Branches),
                    node.RequirementIndex,
                    [.. node.LhsPath],
                    node.IsList,
                    node.SkipOnNull,
                    node.ElementInputType,
                    node.ParentTypeCondition,
                    node.TypeCondition,
                    node.RequiresTypeName));
        }

        return builder.MoveToImmutable();
    }

    private static ImmutableArray<RepresentationShapeBranch> FreezeBranches(
        List<MutableRepresentationShapeBranch>? branches)
    {
        if (branches is null || branches.Count == 0)
        {
            return ImmutableArray<RepresentationShapeBranch>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<RepresentationShapeBranch>(branches.Count);

        foreach (var branch in branches)
        {
            builder.Add(new RepresentationShapeBranch(branch.TypeCondition, Freeze(branch.Children)));
        }

        return builder.MoveToImmutable();
    }

    private static bool IsNonNullPosition(ITypeNode? type)
        => type?.Kind is SyntaxKind.NonNullType;

    private static ITypeNode? GetElementType(ITypeNode? type)
        => type?.IsListType() == true ? type.ElementType() : null;

    // A merged node keeps a single element type. Requirements sharing a node
    // must skip the entity when any of their element positions is non-null,
    // so a non-null element type wins over a nullable one.
    private static ITypeNode? MergeElementInputType(ITypeNode? current, ITypeNode? other)
    {
        if (current is null)
        {
            return other;
        }

        if (other is null || IsNonNullPosition(current))
        {
            return current;
        }

        return IsNonNullPosition(other) ? other : current;
    }

    private sealed class MutableRepresentationShapeNode
    {
        public required string Name { get; init; }

        public required string ResponseName { get; init; }

        public List<MutableRepresentationShapeNode>? Children { get; set; }

        public List<MutableRepresentationShapeBranch>? Branches { get; set; }

        public int RequirementIndex { get; set; } = -1;

        public string[] LhsPath { get; set; } = [];

        public bool IsList { get; set; }

        public bool SkipOnNull { get; set; }

        public ITypeNode? ElementInputType { get; set; }

        public string? ParentTypeCondition { get; set; }

        public string? TypeCondition { get; set; }

        public bool RequiresTypeName { get; set; }
    }

    private sealed class MutableRepresentationShapeBranch
    {
        public required string TypeCondition { get; init; }

        public List<MutableRepresentationShapeNode> Children { get; } = [];
    }
}
