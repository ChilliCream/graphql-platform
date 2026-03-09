using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
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
        SelectionSetNode? selectionSetNode)
    {
        // If we do not have an object at this point, it means the field we're trying to extract
        // did not exist in the source schema response or it was null.
        if (source is not { ValueKind: JsonValueKind.Object })
        {
            if (selectionSetNode is null)
            {
                // The only way that selectionSetNode could be null is in the case of the introspection node.
                // There we don't have any errors to handle, so we should never get here in practice.
                throw new InvalidOperationException($"Expected to have a {nameof(SelectionSetNode)}.");
            }

            // TODO: Maybe we should also attempt to see if we can get an error by going up the path here.
            var error = errorTrie?.FindFirstError() ?? _unexpectedExecutionError;

            return BuildErrorResult(target, selectionSetNode, error);
        }

        foreach (var property in source.EnumerateObject())
        {
            if (!target.TryGetProperty(property.NameSpan, out var resultField))
            {
                continue;
            }

            var selection = resultField.AssertSelection();
            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            // TODO: Eww
            var matchingFieldSelection = selectionSetNode?.Selections
                .OfType<FieldNode>()
                .First(x => x.Alias?.Value == selection.ResponseName || x.Name.Value == selection.ResponseName);

            if (!TryCompleteValue(
                    property.Value,
                    resultField,
                    errorTrieForResponseName,
                    selection,
                    matchingFieldSelection!.SelectionSet!,
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
    /// TODO
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the execution can continue.
    /// <c>false</c>, if the execution needs to be halted.
    /// </returns>
    public bool BuildErrorResult(
        CompositeResultElement target,
        SelectionSetNode selectionSetNode,
        IError error)
    {
        if (_errorHandlingMode is ErrorHandlingMode.Halt)
        {
            ProduceError(error, target.Path);

            return false;
        }

        var parentType = target.Path.IsRoot
            ? target.Operation.RootType
            : target.AssertSelection().Type.NamedType();

        if (CouldFieldHaveOutstandingPartialPatches(parentType))
        {
            // TODO: Needs to handle inline fragments
            foreach (var field in selectionSetNode.Selections.OfType<FieldNode>())
            {
                var responseName = field.Alias?.Value ?? field.Name.Value;

                // TODO: Needs to handle skipped
                if (!target.TryGetProperty(responseName, out var fieldResult)
                    || fieldResult.IsInternal
                    || fieldResult.IsNullOrInvalidated)
                {
                    continue;
                }

                var selection = fieldResult.AssertSelection();
            }
        }
        else
        {
            ProduceError(error, target.Path);

            if (_errorHandlingMode is ErrorHandlingMode.Propagate)
            {
                return false;
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

    private static readonly IError _nonNullViolationError = ErrorBuilder.New()
        .SetMessage("Cannot return null for non-nullable field.")
        .SetCode(ErrorCodes.Execution.NonNullViolation)
        .Build();

    private static readonly IError _unexpectedExecutionError = ErrorBuilder.New()
        .SetMessage("Unexpected Execution Error")
        .Build();

    private bool TryCompleteValue(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        SelectionSetNode selectionSetNode,
        IType type,
        int depth)
    {
        if (type.Kind is TypeKind.NonNull)
        {
            if (source.IsNullOrUndefined())
            {
                var error = errorTrie?.FindFirstError() ?? _nonNullViolationError;

                // If we're in halting mode, we don't care about any special cases, we just produce an error and abort.
                if (_errorHandlingMode is ErrorHandlingMode.Halt)
                {
                    ProduceError(error, target.Path);

                    return false;
                }

                if (CouldFieldHaveOutstandingPartialPatches(type))
                {
                    // TODO: We need to try and do a null-propagation of child properties
                    _store.AddPendingError(target.Path, error, selectionSetNode);

                    return true;
                }
                else
                {
                    ProduceError(error, target.Path);

                    if (_errorHandlingMode is ErrorHandlingMode.Propagate)
                    {
                        return false;
                    }

                    return true;
                }
            }

            type = type.InnerType();
        }

        if (source.IsNullOrUndefined())
        {
            // If the value is null, it might've been nulled due to a down-stream null propagation.
            // So we try to get an error that is associated with this field or with a path below it.
            if (errorTrie?.FindFirstError() is { } error)
            {
                // If we're in halting mode, we don't care about any special cases, we just produce an error and abort.
                if (_errorHandlingMode is ErrorHandlingMode.Halt)
                {
                    ProduceError(error, target.Path);

                    return false;
                }

                // If the field has an error, we need to check if this field is a "shared" field like `viewer`.
                if (CouldFieldHaveOutstandingPartialPatches(type))
                {
                    // TODO: We need to try and do a null-propagation of child properties
                    _store.AddPendingError(target.Path, error, selectionSetNode);
                }
                else
                {
                    // If the field isn't a "shared" field, we can just produce an error for it.
                    ProduceError(error, target.Path);
                }

                return true;
            }

            target.SetNullValue();

            return true;
        }

        switch (type.Kind)
        {
            case TypeKind.List:
                return TryCompleteList(source, target, errorTrie, selection, selectionSetNode, type, depth);

            case TypeKind.Object:
                return TryCompleteObjectValue(selection, selectionSetNode, type, source, errorTrie, depth, target);

            case TypeKind.Interface or TypeKind.Union:
                return TryCompleteAbstractValue(source, target, errorTrie, selection, selectionSetNode, type, depth);

            case TypeKind.Scalar or TypeKind.Enum:
                target.SetLeafValue(source);
                return true;

            default:
                throw new NotSupportedException($"The type {type} is not supported.");
        }
    }

    private void ProduceError(IError error, Path path)
    {
        var errorWithPath = ErrorBuilder.FromError(error)
            .SetPath(path)
            .Build();
        errorWithPath = _errorHandler.Handle(errorWithPath);

        _store.AddError(errorWithPath);
    }

    // TODO: Implement
    private static bool CouldFieldHaveOutstandingPartialPatches(IType type)
    {
        return false;
    }

    private bool TryCompleteList(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        SelectionSetNode selectionSetNode,
        IType type,
        int depth)
    {
        AssertDepthAllowed(ref depth);

        var elementType = type.ElementType();
        var isNullable = elementType.IsNullableType();
        var isLeaf = elementType.IsLeafType();
        var isNested = elementType.IsListType();

        target.SetArrayValue(source.GetArrayLength());

        var i = 0;
        using var enumerator = target.EnumerateArray().GetEnumerator();
        foreach (var element in source.EnumerateArray())
        {
            var success = enumerator.MoveNext();
            Debug.Assert(success, "The lists must have the same size.");

            ErrorTrie? errorTrieForIndex = null;
            errorTrie?.TryGetValue(i, out errorTrieForIndex);

            if (errorTrieForIndex?.Error is { } error)
            {
                var errorWithPath = ErrorBuilder.FromError(error)
                    .SetPath(target.Path.Append(i))
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

                enumerator.Current.SetNullValue();
                goto TryCompleteList_MoveNext;
            }

            if (!HandleElement(element, enumerator.Current, errorTrieForIndex))
            {
                if (!isNullable)
                {
                    return false;
                }

                enumerator.Current.SetNullValue();
                goto TryCompleteList_MoveNext;
            }

TryCompleteList_MoveNext:
            i++;
        }

        return true;

        bool HandleElement(
            SourceResultElement sourceElement,
            CompositeResultElement targetElement,
            ErrorTrie? errorTrieForIndex)
        {
            if (isNested)
            {
                return TryCompleteList(
                    sourceElement,
                    targetElement,
                    errorTrieForIndex,
                    selection,
                    selectionSetNode,
                    elementType,
                    depth);
            }

            if (isLeaf)
            {
                targetElement.SetLeafValue(sourceElement);
                return true;
            }

            if (elementType.IsAbstractType())
            {
                return TryCompleteAbstractValue(
                    sourceElement,
                    targetElement,
                    errorTrieForIndex,
                    selection,
                    selectionSetNode,
                    elementType,
                    depth);
            }

            return TryCompleteObjectValue(
                selection,
                selectionSetNode,
                elementType,
                sourceElement,
                errorTrieForIndex,
                depth,
                targetElement);
        }
    }

    private bool TryCompleteObjectValue(
        Selection parentSelection,
        SelectionSetNode selectionSetNode,
        IType type,
        SourceResultElement source,
        ErrorTrie? errorTrie,
        int depth,
        CompositeResultElement target)
    {
        var namedType = type.NamedType();
        var objectType = Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);

        return TryCompleteObjectValue(source, target, errorTrie, parentSelection, selectionSetNode, objectType, depth);
    }

    private bool TryCompleteObjectValue(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection parentSelection,
        SelectionSetNode selectionSetNode,
        IObjectTypeDefinition objectType,
        int depth)
    {
        AssertDepthAllowed(ref depth);

        // if the property value is yet undefined we need to initialize it
        // with the current selection set.
        if (target.ValueKind is JsonValueKind.Undefined)
        {
            var operation = parentSelection.DeclaringSelectionSet.DeclaringOperation;
            var selectionSet = operation.GetSelectionSet(parentSelection, objectType);
            target.SetObjectValue(selectionSet);
        }

        foreach (var property in source.EnumerateObject())
        {
            if (!target.TryGetProperty(property.NameSpan, out var targetProperty))
            {
                continue;
            }

            var selection = targetProperty.AssertSelection();

            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            if (!TryCompleteValue(
                    property.Value,
                    targetProperty,
                    errorTrieForResponseName,
                    selection,
                    selectionSetNode,
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
        SelectionSetNode selectionSetNode,
        IType type,
        int depth)
        => TryCompleteObjectValue(source, target, errorTrie, selection, selectionSetNode, GetType(type, source), depth);

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
