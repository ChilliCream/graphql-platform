using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed class ValueCompletion
{
    private readonly FetchResultStore _store;
    private readonly ISchemaDefinition _schema;
    private readonly IErrorHandler _errorHandler;
    private readonly ErrorHandlingMode _errorHandlingMode;
    private readonly int _maxDepth;

    public ValueCompletion(
        FetchResultStore store,
        ISchemaDefinition schema,
        IErrorHandler errorHandler,
        ErrorHandlingMode errorHandlingMode,
        int maxDepth)
    {
        ArgumentNullException.ThrowIfNull(schema);

        _store = store;
        _schema = schema;
        _errorHandler = errorHandler;
        _errorHandlingMode = errorHandlingMode;
        _maxDepth = maxDepth;
    }

    /// <summary>
    /// Tries to complete the selection set data represented by <paramref name="target"/>.
    /// <paramref name="source"/>, checking for errors on the <paramref name="errorTrie"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the execution can continue.
    /// <c>false</c>, if the execution needs to be halted.
    /// </returns>
    public bool BuildResult(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        ReadOnlySpan<string> responseNames)
    {
        if (source is not { ValueKind: JsonValueKind.Object })
        {
            var error = errorTrie?.FindFirstError() ??
                ErrorBuilder.New()
                    .SetMessage("Unexpected Execution Error")
                    .Build();

            return BuildErrorResult(target, responseNames, error, target.Path);
        }

        if (target.TryGetSelectionSet(out var targetSelectionSet))
        {
            var selectionSetId = targetSelectionSet.Id;
            var startCursor = target.GetStartCursor();

            if (errorTrie is null)
            {
                foreach (var property in source.EnumerateObject())
                {
                    if (!targetSelectionSet.TryGetSelection(property.NameSpan, out var selection))
                    {
                        continue;
                    }

                    var resultField = target.GetSelectionProperty(selection, selectionSetId, startCursor);
                    var propertyValue = property.Value;

                    if (selection.IsLeafValue && !propertyValue.IsNullOrUndefined())
                    {
                        resultField.SetLeafValue(propertyValue);
                        continue;
                    }

                    if (!TryCompleteValue(propertyValue, resultField, null, selection, selection.Type, 0))
                    {
                        switch (_errorHandlingMode)
                        {
                            case ErrorHandlingMode.Propagate:
                                var didPropagateToRoot = PropagateNullValues(resultField);
                                return !didPropagateToRoot;

                            case ErrorHandlingMode.Halt:
                                return false;
                        }
                    }
                }

                return true;
            }

            foreach (var property in source.EnumerateObject())
            {
                if (!targetSelectionSet.TryGetSelection(property.NameSpan, out var selection))
                {
                    continue;
                }

                var resultField = target.GetSelectionProperty(selection, selectionSetId, startCursor);
                errorTrie.TryGetValue(selection.ResponseName, out var errorTrieForResponseName);
                var propertyValue = property.Value;

                if (!TrySetLeafValueFast(propertyValue, resultField, selection)
                    && !TryCompleteValue(
                        propertyValue,
                        resultField,
                        errorTrieForResponseName,
                        selection,
                        selection.Type,
                        0))
                {
                    switch (_errorHandlingMode)
                    {
                        case ErrorHandlingMode.Propagate:
                            var didPropagateToRoot = PropagateNullValues(resultField);
                            return !didPropagateToRoot;

                        case ErrorHandlingMode.Halt:
                            return false;
                    }
                }
            }

            return true;
        }

        if (errorTrie is null)
        {
            foreach (var property in source.EnumerateObject())
            {
                if (!target.TryGetProperty(property.NameSpan, out var resultField))
                {
                    continue;
                }

                var selection = resultField.AssertSelection();
                var propertyValue = property.Value;

                if (!TrySetLeafValueFast(propertyValue, resultField, selection)
                    && !TryCompleteValue(propertyValue, resultField, null, selection, selection.Type, 0))
                {
                    switch (_errorHandlingMode)
                    {
                        case ErrorHandlingMode.Propagate:
                            var didPropagateToRoot = PropagateNullValues(resultField);
                            return !didPropagateToRoot;

                        case ErrorHandlingMode.Halt:
                            return false;
                    }
                }
            }

            return true;
        }

        foreach (var property in source.EnumerateObject())
        {
            if (!target.TryGetProperty(property.NameSpan, out var resultField))
            {
                continue;
            }

            var selection = resultField.AssertSelection();
            errorTrie.TryGetValue(selection.ResponseName, out var errorTrieForResponseName);
            var propertyValue = property.Value;

            if (!TrySetLeafValueFast(propertyValue, resultField, selection)
                && !TryCompleteValue(
                    propertyValue,
                    resultField,
                    errorTrieForResponseName,
                    selection,
                    selection.Type,
                    0))
            {
                switch (_errorHandlingMode)
                {
                    case ErrorHandlingMode.Propagate:
                        var didPropagateToRoot = PropagateNullValues(resultField);
                        return !didPropagateToRoot;

                    case ErrorHandlingMode.Halt:
                        return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Tries to <c>null</c> and assign the <paramref name="error"/> to the path
    /// of each <paramref name="responseNames"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the execution can continue.
    /// <c>false</c>, if the execution needs to be halted.
    /// </returns>
    public bool BuildErrorResult(
        CompositeResultElement target,
        ReadOnlySpan<string> responseNames,
        IError error,
        Path path)
    {
        foreach (var responseName in responseNames)
        {
            if (!target.TryGetProperty(responseName, out var fieldResult)
                || fieldResult.IsInternal)
            {
                continue;
            }

            var selection = fieldResult.AssertSelection();
            var errorWithPath = ErrorBuilder.FromError(error)
                .SetPath(path.Append(responseName))
                .AddLocation(selection.SyntaxNodes[0].Node)
                .Build();
            errorWithPath = _errorHandler.Handle(errorWithPath);

            _store.AddError(errorWithPath);

            switch (_errorHandlingMode)
            {
                case ErrorHandlingMode.Halt:
                    return false;

                case ErrorHandlingMode.Propagate when selection.Type.Kind is TypeKind.NonNull:
                    var didPropagateToRoot = PropagateNullValues(fieldResult);
                    return !didPropagateToRoot;
            }
        }

        return true;
    }

    /// <summary>
    /// Invalidates the current result and its parents,
    /// until reaching a parent that can be set to <c>null</c>.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the null propagated up to the root.
    /// </returns>
    private static bool PropagateNullValues(CompositeResultElement result)
    {
        var current = result;

        do
        {
            if (current.IsNullable)
            {
                current.SetNullValue();
                return false;
            }

            current.Invalidate();
            current = current.Parent;
        } while (!current.IsNullOrInvalidated);

        return true;
    }

    // TODO: When extracting an error from a path below the current field,
    //       we should try to use the path of the original error if it's
    //       part of what was selected.
    private bool TryCompleteValue(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        IType type,
        int depth)
    {
        var sourceValueKind = source.ValueKind;
        var isNullOrUndefined = sourceValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

        if (type.Kind is TypeKind.NonNull)
        {
            if (isNullOrUndefined)
            {
                IError error;
                if (errorTrie?.FindFirstError() is { } errorFromPath)
                {
                    error = ErrorBuilder.FromError(errorFromPath)
                        .SetPath(target.Path)
                        .AddLocation(selection.SyntaxNodes[0].Node)
                        .Build();
                }
                else
                {
                    error = ErrorBuilder.New()
                        .SetMessage("Cannot return null for non-nullable field.")
                        .SetCode(ErrorCodes.Execution.NonNullViolation)
                        .SetPath(target.Path)
                        .AddLocation(selection.SyntaxNodes[0].Node)
                        .Build();
                }

                error = _errorHandler.Handle(error);

                _store.AddError(error);

                if (_errorHandlingMode is ErrorHandlingMode.Propagate or ErrorHandlingMode.Halt)
                {
                    return false;
                }

                return true;
            }

            type = type.InnerType();
        }

        if (isNullOrUndefined)
        {
            // If the value is null, it might've been nulled due to a
            // down-stream null propagation.
            // So we try to get an error that is associated with this field
            // or with a path below it.
            if (errorTrie?.FindFirstError() is { } error)
            {
                var errorWithPath = ErrorBuilder.FromError(error)
                    .SetPath(target.Path)
                    .AddLocation(selection.SyntaxNodes[0].Node)
                    .Build();
                errorWithPath = _errorHandler.Handle(errorWithPath);

                _store.AddError(errorWithPath);

                if (_errorHandlingMode is ErrorHandlingMode.Halt)
                {
                    return false;
                }
            }
            else
            {
                target.SetNullValue();
            }

            return true;
        }

        switch (type.Kind)
        {
            case TypeKind.List:
                return TryCompleteList(source, target, errorTrie, selection, type, depth);

            case TypeKind.Object:
                return TryCompleteObjectValue(selection, type, source, errorTrie, depth, target);

            case TypeKind.Interface or TypeKind.Union:
                return TryCompleteAbstractValue(source, target, errorTrie, selection, type, depth);

            case TypeKind.Scalar or TypeKind.Enum:
                target.SetLeafValue(source);
                return true;

            default:
                throw new NotSupportedException($"The type {type} is not supported.");
        }
    }

    private bool TryCompleteList(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        IType type,
        int depth)
    {
        AssertDepthAllowed(ref depth);

        var elementType = type.ElementType();
        var isNullable = elementType.IsNullableType();
        var isLeaf = elementType.IsLeafType();
        var isNested = elementType.IsListType();
        var elementKind = isNested
            ? 0
            : isLeaf
                ? 1
                : elementType.IsAbstractType()
                    ? 2
                    : 3;
        IObjectTypeDefinition? objectElementType = null;
        SelectionSet? objectElementSelectionSet = null;

        if (elementKind is 3)
        {
            var namedType = elementType.NamedType();
            objectElementType = Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);
            objectElementSelectionSet = selection.GetSelectionSet(objectElementType);
        }

        target.SetArrayValue(source.GetArrayLength());
        var arrayStartCursor = target.GetStartCursor();

        if (errorTrie is null)
        {
            if (elementKind is 3)
            {
                var elementIndex = 0;
                foreach (var element in source.EnumerateArray())
                {
                    var current = target.GetArrayElement(arrayStartCursor, elementIndex++);

                    if (element.IsNullOrUndefined())
                    {
                        if (!isNullable
                            && _errorHandlingMode is ErrorHandlingMode.Propagate or ErrorHandlingMode.Halt)
                        {
                            return false;
                        }

                        current.SetNullValue();
                        continue;
                    }

                    if (!TryCompleteObjectValue(
                            element,
                            current,
                            null,
                            selection,
                            objectElementType!,
                            objectElementSelectionSet,
                            depth))
                    {
                        if (!isNullable)
                        {
                            return false;
                        }

                        current.SetNullValue();
                    }
                }

                return true;
            }

            var j = 0;
            foreach (var element in source.EnumerateArray())
            {
                var current = target.GetArrayElement(arrayStartCursor, j++);

                if (element.IsNullOrUndefined())
                {
                    if (!isNullable && _errorHandlingMode is ErrorHandlingMode.Propagate or ErrorHandlingMode.Halt)
                    {
                        return false;
                    }

                    current.SetNullValue();
                    continue;
                }

                var success = true;

                switch (elementKind)
                {
                    case 0:
                        success = TryCompleteList(
                            element,
                            current,
                            null,
                            selection,
                            elementType,
                            depth);
                        break;

                    case 1:
                        current.SetLeafValue(element);
                        break;

                    case 2:
                        success = TryCompleteAbstractValue(
                            element,
                            current,
                            null,
                            selection,
                            elementType,
                            depth);
                        break;

                    default:
                        success = TryCompleteObjectValue(
                            element,
                            current,
                            null,
                            selection,
                            objectElementType!,
                            objectElementSelectionSet,
                            depth);
                        break;
                }

                if (!success)
                {
                    if (!isNullable)
                    {
                        return false;
                    }

                    current.SetNullValue();
                }
            }

            return true;
        }

        var i = 0;
        foreach (var element in source.EnumerateArray())
        {
            var current = target.GetArrayElement(arrayStartCursor, i);

            errorTrie.TryGetValue(i, out var errorTrieForIndex);

            if (errorTrieForIndex?.Error is { } error)
            {
                var errorWithPath = ErrorBuilder.FromError(error)
                    .SetPath(target.Path.Append(i))
                    .AddLocation(selection.SyntaxNodes[0].Node)
                    .Build();
                errorWithPath = _errorHandler.Handle(errorWithPath);

                _store.AddError(errorWithPath);

                if (_errorHandlingMode is ErrorHandlingMode.Halt)
                {
                    return false;
                }
            }

            if (element.IsNullOrUndefined())
            {
                if (!isNullable && _errorHandlingMode is ErrorHandlingMode.Propagate or ErrorHandlingMode.Halt)
                {
                    return false;
                }

                current.SetNullValue();
                i++;
                continue;
            }

            var success = true;

            switch (elementKind)
            {
                case 0:
                    success = TryCompleteList(
                        element,
                        current,
                        errorTrieForIndex,
                        selection,
                        elementType,
                        depth);
                    break;

                case 1:
                    current.SetLeafValue(element);
                    break;

                case 2:
                    success = TryCompleteAbstractValue(
                        element,
                        current,
                        errorTrieForIndex,
                        selection,
                        elementType,
                        depth);
                    break;

                default:
                    success = TryCompleteObjectValue(
                        element,
                        current,
                        errorTrieForIndex,
                        selection,
                        objectElementType!,
                        objectElementSelectionSet,
                        depth);
                    break;
            }

            if (!success)
            {
                if (!isNullable)
                {
                    return false;
                }

                current.SetNullValue();
            }

            i++;
        }

        return true;
    }

    private bool TryCompleteObjectValue(
        Selection parentSelection,
        IType type,
        SourceResultElement source,
        ErrorTrie? errorTrie,
        int depth,
        CompositeResultElement target)
    {
        var namedType = type.NamedType();
        var objectType = Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);

        return TryCompleteObjectValue(
            source,
            target,
            errorTrie,
            parentSelection,
            objectType,
            precomputedSelectionSet: null,
            depth);
    }

    private bool TryCompleteObjectValue(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection parentSelection,
        IObjectTypeDefinition objectType,
        SelectionSet? precomputedSelectionSet,
        int depth)
    {
        AssertDepthAllowed(ref depth);
        SelectionSet? selectionSet = null;

        // if the property value is yet undefined we need to initialize it
        // with the current selection set.
        if (target.ValueKind is JsonValueKind.Undefined)
        {
            if (precomputedSelectionSet is null)
            {
                precomputedSelectionSet = parentSelection.GetSelectionSet(objectType);
            }

            selectionSet = precomputedSelectionSet;
            target.SetObjectValue(selectionSet);
        }
        else if (target.TryGetSelectionSet(out var existingSelectionSet))
        {
            selectionSet = existingSelectionSet;
        }

        if (selectionSet is not null)
        {
            var selectionSetId = selectionSet.Id;
            var startCursor = target.GetStartCursor();

            if (errorTrie is null)
            {
                foreach (var property in source.EnumerateObject())
                {
                    if (!selectionSet.TryGetSelection(property.NameSpan, out var selection))
                    {
                        continue;
                    }

                    var targetProperty = target.GetSelectionProperty(selection, selectionSetId, startCursor);
                    var propertyValue = property.Value;

                    if (selection.IsLeafValue && !propertyValue.IsNullOrUndefined())
                    {
                        targetProperty.SetLeafValue(propertyValue);
                        continue;
                    }

                    if (!TryCompleteValue(
                        propertyValue,
                        targetProperty,
                        null,
                        selection,
                        selection.Type,
                        depth))
                    {
                        return false;
                    }
                }

                return true;
            }

            foreach (var property in source.EnumerateObject())
            {
                if (!selectionSet.TryGetSelection(property.NameSpan, out var selection))
                {
                    continue;
                }

                var targetProperty = target.GetSelectionProperty(selection, selectionSetId, startCursor);
                errorTrie.TryGetValue(selection.ResponseName, out var errorTrieForResponseName);
                var propertyValue = property.Value;

                if (!TrySetLeafValueFast(propertyValue, targetProperty, selection)
                    && !TryCompleteValue(
                        propertyValue,
                        targetProperty,
                        errorTrieForResponseName,
                        selection,
                        selection.Type,
                        depth))
                {
                    return false;
                }
            }

            return true;
        }

        if (errorTrie is null)
        {
            foreach (var property in source.EnumerateObject())
            {
                if (!target.TryGetProperty(property.NameSpan, out var targetProperty))
                {
                    continue;
                }

                var selection = targetProperty.AssertSelection();
                var propertyValue = property.Value;

                if (!TrySetLeafValueFast(propertyValue, targetProperty, selection)
                    && !TryCompleteValue(
                        propertyValue,
                        targetProperty,
                        null,
                        selection,
                        selection.Type,
                        depth))
                {
                    return false;
                }
            }

            return true;
        }

        foreach (var property in source.EnumerateObject())
        {
            if (!target.TryGetProperty(property.NameSpan, out var targetProperty))
            {
                continue;
            }

            var selection = targetProperty.AssertSelection();
            errorTrie.TryGetValue(selection.ResponseName, out var errorTrieForResponseName);
            var propertyValue = property.Value;

            if (!TrySetLeafValueFast(propertyValue, targetProperty, selection)
                && !TryCompleteValue(
                    propertyValue,
                    targetProperty,
                    errorTrieForResponseName,
                    selection,
                    selection.Type,
                    depth))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryCompleteAbstractValue(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        IType type,
        int depth)
        => TryCompleteObjectValue(
            source,
            target,
            errorTrie,
            selection,
            GetType(type, source),
            precomputedSelectionSet: null,
            depth);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IObjectTypeDefinition GetType(IType type, SourceResultElement data)
    {
        var namedType = type.NamedType();

        if (namedType is IObjectTypeDefinition objectType)
        {
            return objectType;
        }

        var typeName = data.GetProperty(IntrospectionFieldNames.TypeNameSpan).AssertString();
        return _schema.Types.GetType<IObjectTypeDefinition>(typeName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TrySetLeafValueFast(
        SourceResultElement source,
        CompositeResultElement target,
        Selection selection)
    {
        if (selection.IsLeafValue && !source.IsNullOrUndefined())
        {
            target.SetLeafValue(source);
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertDepthAllowed(ref int depth)
    {
        depth++;

        if (depth > _maxDepth)
        {
            throw new NotSupportedException($"The depth {depth} is not allowed.");
        }
    }
}

file static class ValueCompletionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrUndefined(this SourceResultElement element)
        => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;
}
