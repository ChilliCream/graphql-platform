using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
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
    /// The schema against which the requirement input types are resolved to
    /// determine which nodes cannot be satisfied by a null value.
    /// </param>
    /// <returns>The root level of the representation shape.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a requirement is not bound by exactly one argument of the
    /// lookup selection, or when requirement maps produce conflicting nodes.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when a requirement map uses a value selection construct that
    /// cannot be represented in a representation object.
    /// </exception>
    public static List<RepresentationShapeNode> Build(
        FieldNode lookupField,
        ReadOnlySpan<OperationRequirement> requiredData,
        ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(lookupField);
        ArgumentNullException.ThrowIfNull(schema);

        var root = new List<RepresentationShapeNode>();
        var matched = requiredData.Length <= 64
            ? stackalloc bool[requiredData.Length]
            : new bool[requiredData.Length];
        matched.Clear();

        AddRequirementArguments(lookupField, root, requiredData, matched, schema);
        WalkSelections(lookupField.SelectionSet, root, requiredData, matched, schema);

        for (var i = 0; i < requiredData.Length; i++)
        {
            if (!matched[i])
            {
                throw new InvalidOperationException(
                    $"The lookup selection does not bind the requirement '{requiredData[i].Key}' "
                    + "to an argument.");
            }
        }

        return root;
    }

    private static void WalkSelections(
        SelectionSetNode? selectionSet,
        List<RepresentationShapeNode> level,
        ReadOnlySpan<OperationRequirement> requiredData,
        Span<bool> matched,
        ISchemaDefinition schema)
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
                    AddRequirementArguments(field, level, requiredData, matched, schema);

                    if (HasRequirementArguments(field.SelectionSet, requiredData))
                    {
                        var node = GetOrCreateStructuralNode(level, field);
                        WalkSelections(field.SelectionSet, node.Children!, requiredData, matched, schema);
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    // An inline fragment adds no level to the result data, so its
                    // selections contribute to the current level.
                    WalkSelections(inlineFragment.SelectionSet, level, requiredData, matched, schema);
                    break;
            }
        }
    }

    private static void AddRequirementArguments(
        FieldNode field,
        List<RepresentationShapeNode> level,
        ReadOnlySpan<OperationRequirement> requiredData,
        Span<bool> matched,
        ISchemaDefinition schema)
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
            AddValueSelection(
                level,
                requiredData[index].Map,
                index,
                [],
                requiredData[index].ResolveInputType(schema));
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
        IType? inputType)
    {
        switch (selection)
        {
            case PathNode path:
                AddPath(level, path, requirementIndex, lhsPath, inputType);
                break;

            case ObjectValueSelectionNode objectValue:
                AddObjectFields(level, objectValue, requirementIndex, lhsPath, inputType);
                break;

            case PathObjectValueSelectionNode pathObject:
                var terminal = EnsureCompositeChain(level, pathObject.Path);
                AddObjectFields(
                    terminal.Children!,
                    pathObject.ObjectValueSelection,
                    requirementIndex,
                    lhsPath,
                    inputType);
                break;

            case PathListValueSelectionNode pathList:
                AddPathList(level, pathList, requirementIndex, lhsPath, inputType);
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
        IType? inputType)
    {
        var currentLevel = level;
        var parentTypeCondition = path.TypeName?.Value;
        var segment = path.PathSegment;

        while (segment.PathSegment is not null)
        {
            // A null intermediate of a plain path resolves the leaf value to
            // null, so the node must not make the entity unresolvable.
            var node = GetOrCreateCompositeNode(
                currentLevel,
                segment.FieldName.Value,
                parentTypeCondition,
                segment.TypeName?.Value,
                skipOnNull: false);
            currentLevel = node.Children!;
            parentTypeCondition = null;
            segment = segment.PathSegment;
        }

        AddLeafNode(
            currentLevel,
            segment.FieldName.Value,
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
        IType? inputType)
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

        while (segment.PathSegment is not null)
        {
            // A null intermediate of a list path resolves the list value to
            // null, so the node must not make the entity unresolvable.
            var node = GetOrCreateCompositeNode(
                currentLevel,
                segment.FieldName.Value,
                parentTypeCondition,
                segment.TypeName?.Value,
                skipOnNull: false);
            currentLevel = node.Children!;
            parentTypeCondition = null;
            segment = segment.PathSegment;
        }

        var elementType = GetElementType(inputType);
        var existing = FindNode(currentLevel, segment.FieldName.Value);

        if (existing is not null)
        {
            if (!existing.IsList)
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

        var listNode = CreateNode(segment.FieldName.Value, segment.FieldName.Value);
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
        AddObjectFields(listNode.Children, elementSelection, requirementIndex, [], elementType);
    }

    private static void AddObjectFields(
        List<RepresentationShapeNode> level,
        ObjectValueSelectionNode objectValue,
        int requirementIndex,
        List<string> lhsPath,
        IType? inputType)
    {
        var inputObjectType = GetInputObjectType(inputType);

        foreach (var field in objectValue.Fields)
        {
            lhsPath.Add(field.Name.Value);

            IType? fieldType = null;

            if (inputObjectType is not null
                && inputObjectType.Fields.TryGetField(field.Name.Value, out var inputField))
            {
                fieldType = inputField.Type;
            }

            if (field.ValueSelection is null)
            {
                AddLeafNode(
                    level,
                    field.Name.Value,
                    requirementIndex,
                    lhsPath,
                    parentTypeCondition: null,
                    typeCondition: null,
                    fieldType);
            }
            else
            {
                AddValueSelection(level, field.ValueSelection, requirementIndex, lhsPath, fieldType);
            }

            lhsPath.RemoveAt(lhsPath.Count - 1);
        }
    }

    private static RepresentationShapeNode EnsureCompositeChain(
        List<RepresentationShapeNode> level,
        PathNode path)
    {
        var currentLevel = level;
        var parentTypeCondition = path.TypeName?.Value;
        var segment = path.PathSegment;

        while (true)
        {
            // An object value selection is unresolvable when any segment of
            // its path is null, so the whole chain skips the entity on null.
            var node = GetOrCreateCompositeNode(
                currentLevel,
                segment.FieldName.Value,
                parentTypeCondition,
                segment.TypeName?.Value,
                skipOnNull: true);

            if (segment.PathSegment is null)
            {
                return node;
            }

            currentLevel = node.Children!;
            parentTypeCondition = null;
            segment = segment.PathSegment;
        }
    }

    // A structural node always emits an object. A list-valued structural field is
    // not representable here, but the planner re-roots nested requirements at
    // depth 1, so list-shaped structural parents do not occur; the builder is
    // type-blind and cannot guard against them.
    private static RepresentationShapeNode GetOrCreateStructuralNode(
        List<RepresentationShapeNode> level,
        FieldNode field)
    {
        var name = field.Name.Value;
        var responseName = field.Alias?.Value ?? name;
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
            return existing;
        }

        var node = CreateNode(name, responseName);
        node.Children = [];
        node.SkipOnNull = true;
        level.Add(node);
        return node;
    }

    private static RepresentationShapeNode GetOrCreateCompositeNode(
        List<RepresentationShapeNode> level,
        string name,
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

            if (!string.Equals(existing.ResponseName, name, StringComparison.Ordinal)
                || !string.Equals(existing.ParentTypeCondition, parentTypeCondition, StringComparison.Ordinal)
                || !string.Equals(existing.TypeCondition, typeCondition, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The requirement maps produce conflicting representation nodes for '{name}'.");
            }

            // When requirements of both null behaviors merge under one node,
            // skip-on-null wins: the skipping requirement is unresolvable on null.
            existing.SkipOnNull |= skipOnNull;
            return existing;
        }

        var node = CreateNode(name, name);
        node.Children = [];
        node.ParentTypeCondition = parentTypeCondition;
        node.TypeCondition = typeCondition;
        node.SkipOnNull = skipOnNull;
        level.Add(node);
        return node;
    }

    private static void AddLeafNode(
        List<RepresentationShapeNode> level,
        string name,
        int requirementIndex,
        List<string> lhsPath,
        string? parentTypeCondition,
        string? typeCondition,
        IType? inputType)
    {
        var skipOnNull = IsNonNullPosition(inputType);
        var elementInputType = GetElementType(inputType);
        var existing = FindNode(level, name);

        if (existing is not null)
        {
            if (existing.Children is not null)
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

        var node = CreateNode(name, name);
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

    private static bool IsNonNullPosition(IType? type)
        => type?.Kind is TypeKind.NonNull;

    private static IType? GetElementType(IType? type)
    {
        if (type is NonNullType nonNullType)
        {
            type = nonNullType.NullableType;
        }

        return type is ListType listType ? listType.ElementType : null;
    }

    // A merged node keeps a single element type. Requirements sharing a node
    // must skip the entity when any of their element positions is non-null,
    // so a non-null element type wins over a nullable one.
    private static IType? MergeElementInputType(IType? current, IType? other)
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

    private static IInputObjectTypeDefinition? GetInputObjectType(IType? type)
        => type?.AsTypeDefinition() as IInputObjectTypeDefinition;
}
