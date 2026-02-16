using System.Diagnostics;
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

        foreach (var property in source.EnumerateObject())
        {
            if (!target.TryGetProperty(property.NameSpan, out var resultField))
            {
                continue;
            }

            var selection = resultField.AssertSelection();
            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            if (!TryCompleteValue(property.Value, resultField, errorTrieForResponseName, selection, selection.Type, 0))
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
        if (type.Kind is TypeKind.NonNull)
        {
            if (source.IsNullOrUndefined())
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

        if (source.IsNullOrUndefined())
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
                return TryCompleteAbstractValue(sourceElement,
                    targetElement, errorTrieForIndex, selection, elementType, depth);
            }

            return TryCompleteObjectValue(
                selection,
                elementType,
                sourceElement,
                errorTrieForIndex,
                depth,
                targetElement);
        }
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

        return TryCompleteObjectValue(source, target, errorTrie, parentSelection, objectType, depth);
    }

    private bool TryCompleteObjectValue(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection parentSelection,
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

            if (!TryCompleteValue(property.Value,
                targetProperty, errorTrieForResponseName, selection, selection.Type, depth))
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
        => TryCompleteObjectValue(source, target, errorTrie, selection, GetType(type, source), depth);

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
