using System.Collections.Immutable;
using System.Text;
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
    /// The root level of the representation shape.
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
        => Build(BindRepresentationPaths(lookupField, requiredData), schema, entityTypeName);

    /// <summary>
    /// Binds the representation path of every operation requirement of a lookup field.
    /// </summary>
    /// <param name="lookupField">The original, un-stripped root lookup field.</param>
    /// <param name="requiredData">The operation requirements of the lookup.</param>
    /// <returns>
    /// The requirements in their original order, each carrying the path of the
    /// selection set that binds it.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a requirement is not bound by exactly one argument of the
    /// lookup selection.
    /// </exception>
    public static OperationRequirement[] BindRepresentationPaths(
        FieldNode lookupField,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        ArgumentNullException.ThrowIfNull(lookupField);

        var bound = new OperationRequirement[requiredData.Length];
        var path = new List<RepresentationPathSegment>();

        BindArguments(lookupField, path, bound, requiredData);
        WalkSelections(lookupField.SelectionSet, path, bound, requiredData);

        for (var i = 0; i < bound.Length; i++)
        {
            if (bound[i] is null)
            {
                throw new InvalidOperationException(
                    $"The lookup selection does not bind the requirement '{requiredData[i].Key}' "
                    + "to an argument.");
            }
        }

        return bound;
    }

    /// <summary>
    /// Builds the representation shape from the bound operation requirements of a lookup.
    /// </summary>
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
    /// The root level of the representation shape.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when requirement maps produce conflicting nodes.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when a requirement map uses a value selection construct that
    /// cannot be represented in a representation object.
    /// </exception>
    public static ImmutableArray<RepresentationShapeNode> Build(
        ReadOnlySpan<OperationRequirement> requiredData,
        FusionSchemaDefinition schema,
        string entityTypeName)
    {
        var root = new List<RepresentationShapeNode>();

        for (var i = 0; i < requiredData.Length; i++)
        {
            var requirement = requiredData[i];
            var level = root;

            foreach (var segment in requirement.RepresentationPath)
            {
                level = GetOrCreateStructuralNode(level, segment);
            }

            AddValueSelection(
                level,
                requirement.Map,
                i,
                [],
                requirement.Type,
                requirement.InternalAlias);
        }

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

        return [.. root];
    }

    // Walks the built shape against the declared types, flagging every
    // non-branched composite node whose declared type is abstract so the
    // emitter writes its runtime __typename. Branched nodes already carry an
    // unconditional __typename via their branch handling and are left unflagged.
    private static void AnnotateAbstractComposites(
        List<RepresentationShapeNode> level,
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

    private static void WalkSelections(
        SelectionSetNode? selectionSet,
        List<RepresentationPathSegment> path,
        OperationRequirement[] bound,
        ReadOnlySpan<OperationRequirement> requiredData)
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
                    BindArguments(field, path, bound, requiredData);

                    if (HasRequirementArguments(field.SelectionSet, requiredData))
                    {
                        path.Add(new RepresentationPathSegment(
                            field.Name.Value,
                            field.Alias?.Value ?? field.Name.Value));
                        WalkSelections(field.SelectionSet, path, bound, requiredData);
                        path.RemoveAt(path.Count - 1);
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    // An inline fragment adds no level to the result data, so its
                    // selections contribute to the current level.
                    WalkSelections(inlineFragment.SelectionSet, path, bound, requiredData);
                    break;
            }
        }
    }

    private static void BindArguments(
        FieldNode field,
        List<RepresentationPathSegment> path,
        OperationRequirement[] bound,
        ReadOnlySpan<OperationRequirement> requiredData)
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

            if (bound[index] is not null)
            {
                throw new InvalidOperationException(
                    $"The lookup selection binds the requirement '{requiredData[index].Key}' "
                    + "to more than one argument.");
            }

            bound[index] = requiredData[index] with { RepresentationPath = [.. path] };
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
        List<RepresentationShapeNode> level,
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
        List<RepresentationShapeNode> level,
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
        List<RepresentationShapeNode> level,
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
        List<RepresentationShapeNode> level,
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

    private static List<RepresentationShapeNode> EnsureCompositeChain(
        List<RepresentationShapeNode> level,
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
    private static List<RepresentationShapeNode> GetOrCreateStructuralNode(
        List<RepresentationShapeNode> level,
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

    private static List<RepresentationShapeNode> GetOrCreateCompositeNode(
        List<RepresentationShapeNode> level,
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

    private static RepresentationShapeBranch GetOrCreateBranch(
        RepresentationShapeNode node,
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

        var branch = new RepresentationShapeBranch { TypeCondition = typeCondition };
        branches.Add(branch);
        return branch;
    }

    private static void AddLeafNode(
        List<RepresentationShapeNode> level,
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

    private static RepresentationShapeNode CreateNode(string name, string responseName)
    {
        var nameUtf8 = Encoding.UTF8.GetBytes(name);

        return new RepresentationShapeNode
        {
            Name = name,
            NameUtf8 = nameUtf8,
            ResponseName = responseName,
            ResponseNameUtf8 = string.Equals(name, responseName, StringComparison.Ordinal)
                ? nameUtf8
                : Encoding.UTF8.GetBytes(responseName)
        };
    }

    private static RepresentationShapeNode? FindNode(
        List<RepresentationShapeNode> level,
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
}
