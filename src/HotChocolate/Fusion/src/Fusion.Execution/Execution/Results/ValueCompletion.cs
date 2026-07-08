using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
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
    private readonly bool _propagateNullValues;
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
        _propagateNullValues = errorHandlingMode is not ErrorHandlingMode.Null;
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
        ResultSelectionSet resultSelectionSet)
    {
        var sourceValueKind = source.ValueKind;

        if (sourceValueKind is not JsonValueKind.Object)
        {
            var error = errorTrie?.FindFirstError();
            var canExecutionContinue =
                BuildResultForInvalidSource(
                    source,
                    sourceValueKind,
                    target,
                    resultSelectionSet,
                    error);

            if (!canExecutionContinue)
            {
                return false;
            }

            return ApplyPocketedErrors(target);
        }

        if (target.ValueKind is JsonValueKind.Undefined)
        {
            InitializeTargetObject(source, target);
        }

        foreach (var property in source.EnumerateObject())
        {
            if (!target.TryGetProperty(property.NameSpan, out var resultField))
            {
                continue;
            }

            var propertyValue = property.Value;
            var propertyValueKind = propertyValue.ValueKind;

            // Fast path: when there are no errors and the source value is a
            // scalar (string, number, bool) we can set it directly without
            // going through the full TryCompleteValue type-dispatch chain.
            if (errorTrie is null && propertyValueKind.IsScalarValue())
            {
                if (propertyValueKind is JsonValueKind.String && resultField.IsEnumValue)
                {
                    CompleteEnumValue(propertyValue, resultField, resultField.AssertSelection());
                    continue;
                }

                resultField.SetLeafValue(propertyValue);
                continue;
            }

            var selection = resultField.AssertSelection();
            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            var childSet = resultSelectionSet.TryGetChild(selection.ResponseName);
            if (!TryCompleteValue(
                    propertyValue,
                    propertyValueKind,
                    resultField,
                    errorTrieForResponseName,
                    selection,
                    selection.Type,
                    0,
                    childSet))
            {
                switch (_errorHandlingMode)
                {
                    case ErrorHandlingMode.Propagate:
                        var didPropagateToRoot = PropagateNullValues(resultField);
                        if (didPropagateToRoot)
                        {
                            return false;
                        }

                        return ApplyPocketedErrors(target);

                }
            }
        }

        return ApplyPocketedErrors(target);
    }

    private void InitializeTargetObject(
        SourceResultElement source,
        CompositeResultElement target)
    {
        if (!TryGetSelectionContext(target, out var selection, out var type)
            || !type.IsCompositeType())
        {
            throw new InvalidOperationException(
                "Cannot initialize a result object without selection metadata.");
        }

        var objectType = GetType(type, source);
        var objectSelectionSet = selection.GetSelectionSet(objectType)!;

        target.SetObjectValue(objectSelectionSet);
    }

    /// <summary>
    /// Tries to <c>null</c> and assign the <paramref name="error"/> to the path
    /// of each field selected by <paramref name="resultSelectionSet"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the execution can continue.
    /// <c>false</c>, if the execution needs to be halted.
    /// </returns>
    public bool BuildErrorResult(
        CompositeResultElement target,
        ResultSelectionSet resultSelectionSet,
        IError error,
        CompactPath path)
    {
        var operation = target.Operation;
        var errorPath = path.ToPath(operation);

        foreach (var responseName in resultSelectionSet.ResponseNames)
        {
            // A prior field's error may have propagated a null up to this target
            // (a non-null field on a nullable parent nulls the parent). Once the
            // target is itself null or invalidated, the remaining field results
            // have nowhere to land, so stop applying them.
            if (target.IsNullOrInvalidated)
            {
                return true;
            }

            if (!target.TryGetProperty(responseName, out var fieldResult)
                || fieldResult.IsInternal)
            {
                continue;
            }

            var selection = fieldResult.AssertSelection();

            if (!ApplyFieldError(fieldResult, selection, error, errorPath.Append(responseName)))
            {
                return false;
            }
        }

        return true;
    }

    public void FinalizePocketedErrors(CompositeResultElement resultData)
    {
        if (!_store.HasPocketedErrors)
        {
            return;
        }

        if (!ApplyPocketedErrors(resultData))
        {
            return;
        }

        if (!_store.HasPocketedErrors)
        {
            return;
        }

        foreach (var (path, error) in _store.GetPocketedErrorsSnapshot())
        {
            var parentPath = path.Parent;

            if (!_store.TryGetResult(parentPath, out var parentResult))
            {
                _store.RemovePocketedError(path);
                continue;
            }

            if (parentResult.IsNullOrInvalidated)
            {
                _store.RemovePocketedErrorsInSubtree(parentPath);
                continue;
            }

            if (parentResult.ValueKind is JsonValueKind.Undefined)
            {
                var promotedError = ErrorBuilder.FromError(error)
                    .SetPath(parentPath);

                _store.AddError(_errorHandler.Handle(promotedError.Build()));
                parentResult.SetNullValue();
                _store.RemovePocketedErrorsInSubtree(parentPath);
                continue;
            }

            _store.RemovePocketedError(path);
        }
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

    private bool BuildResultForInvalidSource(
        SourceResultElement source,
        JsonValueKind sourceValueKind,
        CompositeResultElement target,
        ResultSelectionSet resultSelectionSet,
        IError? error)
    {
        if (sourceValueKind is JsonValueKind.Null && IsValueType(target.Type))
        {
            if (error is not null)
            {
                PocketErrors(target.Path, resultSelectionSet, error);
            }

            return true;
        }

        // A clean null source without an associated error is a legitimate
        // "not found" state (e.g. a lookup that resolved to null), so each
        // selected field is completed with its own nullability semantics
        // instead of surfacing an unexpected execution error.
        if (sourceValueKind is JsonValueKind.Null && error is null)
        {
            return CompleteNullSource(source, target, resultSelectionSet);
        }

        var fallbackError = error ??
            ErrorBuilder.New()
                .SetMessage("Unexpected Execution Error")
                .Build();

        if (target.ValueKind is JsonValueKind.Undefined
            && !TryInitializeTargetObject(target))
        {
            return BuildErrorResultForUndefinedTarget(
                target,
                resultSelectionSet,
                fallbackError,
                target.CompactPath);
        }

        return BuildErrorResult(target, resultSelectionSet, fallbackError, target.CompactPath);
    }

    /// <summary>
    /// Completes the selection set of <paramref name="target"/> for a <c>null</c>
    /// source, applying per-field nullability so that non-null fields produce a
    /// non-null violation and nullable fields are set to <c>null</c>.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the execution can continue.
    /// <c>false</c>, if the execution needs to be halted.
    /// </returns>
    private bool CompleteNullSource(
        SourceResultElement source,
        CompositeResultElement target,
        ResultSelectionSet resultSelectionSet)
    {
        foreach (var responseName in resultSelectionSet.ResponseNames)
        {
            if (!target.TryGetProperty(responseName, out var fieldResult)
                || fieldResult.IsInternal)
            {
                continue;
            }

            var selection = fieldResult.AssertSelection();
            var childSet = resultSelectionSet.TryGetChild(selection.ResponseName);

            if (!TryCompleteValue(
                    source,
                    JsonValueKind.Null,
                    fieldResult,
                    errorTrie: null,
                    selection,
                    selection.Type,
                    0,
                    childSet))
            {
                switch (_errorHandlingMode)
                {
                    case ErrorHandlingMode.Propagate:
                        var didPropagateToRoot = PropagateNullValues(fieldResult);
                        if (didPropagateToRoot)
                        {
                            return false;
                        }

                        return ApplyPocketedErrors(target);
                }
            }
        }

        return ApplyPocketedErrors(target);
    }

    private static bool TryInitializeTargetObject(CompositeResultElement target)
    {
        if (!TryGetSelectionContext(target, out var selection, out var type)
            || type.NamedType() is not IObjectTypeDefinition objectType)
        {
            return false;
        }

        if (selection.IsLeaf)
        {
            return false;
        }

        target.SetObjectValue(selection.GetSelectionSet(objectType)!);
        return true;
    }

    private static bool TryGetSelectionContext(
        CompositeResultElement target,
        [NotNullWhen(true)] out Selection? selection,
        [NotNullWhen(true)] out IType? type)
    {
        type = target.Type;

        if (type is null)
        {
            selection = null;
            return false;
        }

        var current = target;
        while ((selection = current.Selection) is null)
        {
            var parent = current.Parent;
            if (parent.IsNullOrInvalidated)
            {
                type = null;
                return false;
            }

            current = parent;
        }

        return true;
    }

    private bool BuildErrorResultForUndefinedTarget(
        CompositeResultElement target,
        ResultSelectionSet resultSelectionSet,
        IError error,
        CompactPath path)
    {
        var operation = target.Operation;
        var errorPath = path.ToPath(operation);
        var hasResponseNames = false;

        foreach (var responseName in resultSelectionSet.ResponseNames)
        {
            hasResponseNames = true;
            var errorWithPath = ErrorBuilder.FromError(error)
                .SetPath(errorPath.Append(responseName))
                .Build();

            _store.AddError(_errorHandler.Handle(errorWithPath));
        }

        if (!hasResponseNames)
        {
            var errorWithPath = ErrorBuilder.FromError(error)
                .SetPath(errorPath)
                .Build();

            _store.AddError(_errorHandler.Handle(errorWithPath));
        }

        return true;
    }

    private bool ApplyPocketedErrors(CompositeResultElement target)
    {
        if (!_store.HasPocketedErrors)
        {
            return true;
        }

        var targetPath = target.Path;

        foreach (var (path, error) in _store.GetPocketedErrorsSnapshot())
        {
            if (!PathUtilities.IsPathInSubtree(path, targetPath, includeSelf: true))
            {
                continue;
            }

            if (!TryApplyPocketedError(path, error))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryApplyPocketedError(Path path, IError error)
    {
        if (!_store.TryGetResult(path, out var fieldResult))
        {
            return true;
        }

        if (fieldResult.IsNullOrInvalidated)
        {
            _store.RemovePocketedError(path);
            return true;
        }

        if (fieldResult.Selection is not { } selection)
        {
            _store.RemovePocketedError(path);
            return true;
        }

        var canExecutionContinue = ApplyFieldError(fieldResult, selection, error, path);
        _store.RemovePocketedError(path);
        return canExecutionContinue;
    }

    private bool ApplyFieldError(
        CompositeResultElement fieldResult,
        Selection selection,
        IError error,
        Path path)
    {
        var errorWithPath = ErrorBuilder.FromError(error)
            .SetPath(path)
            .Build();
        errorWithPath = _errorHandler.Handle(errorWithPath);

        _store.AddError(errorWithPath);

        switch (_errorHandlingMode)
        {
            case ErrorHandlingMode.Propagate when selection.Type.Kind is TypeKind.NonNull:
                var didPropagateToRoot = PropagateNullValues(fieldResult);
                return !didPropagateToRoot;
        }

        return true;
    }

    private void PocketErrors(Path path, ResultSelectionSet resultSelectionSet, IError error)
    {
        foreach (var responseName in resultSelectionSet.ResponseNames)
        {
            _store.PocketError(path.Append(responseName), error);
        }
    }

    private static bool IsValueType(IType? type)
    {
        if (type is null)
        {
            return false;
        }

        var namedType = type.NamedType();

        return namedType switch
        {
            FusionObjectTypeDefinition { IsValueType: true } => true,
            FusionInterfaceTypeDefinition { IsValueType: true } => true,
            FusionUnionTypeDefinition { IsValueType: true } => true,
            _ => false
        };
    }

    // TODO: When extracting an error from a path below the current field,
    //       we should try to use the path of the original error if it's
    //       part of what was selected.
    private bool TryCompleteValue(
        SourceResultElement source,
        JsonValueKind sourceValueKind,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        IType type,
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        var isNullOrUndefined = sourceValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

        if (type.Kind is TypeKind.NonNull)
        {
            if (isNullOrUndefined)
            {
                IError error;
                if (errorTrie?.FindFirstError() is { } errorFromPath)
                {
                    var path = target.CompactPath.ToPath(target.Operation);
                    error = ErrorBuilder.FromError(errorFromPath)
                        .SetPath(path)
                        .Build();
                }
                else
                {
                    var path = target.CompactPath.ToPath(target.Operation);
                    error = ErrorBuilder.New()
                        .SetMessage("Cannot return null for non-nullable field.")
                        .SetCode(ErrorCodes.Execution.NonNullViolation)
                        .SetPath(path)
                        .Build();
                }

                error = _errorHandler.Handle(error);

                _store.AddError(error);

                return !_propagateNullValues;
            }

            type = type.InnerType();
        }

        if (isNullOrUndefined)
        {
            if (sourceValueKind is JsonValueKind.Null && IsValueType(type))
            {
                if (errorTrie?.FindFirstError() is { } error
                    && resultSelectionSet is not null)
                {
                    PocketErrors(target.Path, resultSelectionSet, error);
                }

                // For shared parent types we keep the target untouched so that
                // sibling subgraph results can still initialize and populate it.
                return true;
            }

            // If the value is null, it might've been nulled due to a
            // down-stream null propagation.
            // So we try to get an error that is associated with this field
            // or with a path below it.
            if (errorTrie?.FindFirstError() is { } errorFromPath)
            {
                var errorWithPath = ErrorBuilder.FromError(errorFromPath)
                    .SetPath(target.Path)
                    .Build();
                errorWithPath = _errorHandler.Handle(errorWithPath);

                _store.AddError(errorWithPath);
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
                return TryCompleteList(
                    source,
                    target,
                    errorTrie,
                    selection,
                    type,
                    depth,
                    resultSelectionSet);

            case TypeKind.Object:
                return TryCompleteObjectValue(
                    selection,
                    type,
                    source,
                    errorTrie,
                    depth,
                    target,
                    resultSelectionSet);

            case TypeKind.Interface or TypeKind.Union:
                return TryCompleteAbstractValue(
                    source,
                    target,
                    errorTrie,
                    selection,
                    type,
                    depth,
                    resultSelectionSet);

            case TypeKind.Scalar:
                target.SetLeafValue(source);
                return true;

            case TypeKind.Enum:
                CompleteEnumValue(source, target, selection);
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
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        AssertDepthAllowed(ref depth);

        var elementType = type.ElementType();
        var elementTypeKind = elementType.Kind;
        var isNonNull = elementTypeKind is TypeKind.NonNull;

        if (isNonNull)
        {
            elementTypeKind = Unsafe.As<IType, NonNullType>(ref elementType).NullableType.Kind;
        }

        // A shared list slot may already be populated by a sibling subgraph
        // result. Create the array only on the first write; otherwise reuse it
        // so sibling contributions accumulate through the positional merge below.
        if (target.ValueKind is JsonValueKind.Undefined)
        {
            target.SetArrayValue(source.GetArrayLength());
        }
        else if (target.ValueKind is not JsonValueKind.Array
            || target.GetArrayLength() != source.GetArrayLength())
        {
            // Non-keyed sibling lists can only be merged by position, which
            // requires an identical length. A differing shape cannot be
            // correlated, so surface an execution error and let the configured
            // null handling apply instead of silently misaligning elements.
            var error = ErrorBuilder.New()
                .SetMessage("Cannot merge shared list results with different lengths.")
                .SetPath(target.CompactPath.ToPath(target.Operation))
                .Build();
            error = _errorHandler.Handle(error);
            _store.AddError(error);

            return !_propagateNullValues;
        }

        var i = 0;
        using var targetEnumerator = target.EnumerateArray().GetEnumerator();
        foreach (var element in source.EnumerateArray())
        {
            var success = targetEnumerator.MoveNext();
            Debug.Assert(success, "The lists must have the same size.");

            ErrorTrie? errorTrieForIndex = null;
            errorTrie?.TryGetValue(i, out errorTrieForIndex);

            if (errorTrieForIndex?.Error is { } error)
            {
                var errorWithPath = ErrorBuilder.FromError(error)
                    .SetPath(target.CompactPath.ToPath(target.Operation, i))
                    .Build();
                errorWithPath = _errorHandler.Handle(errorWithPath);

                _store.AddError(errorWithPath);
            }

            var elementValueKind = element.ValueKind;
            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (isNonNull && _propagateNullValues)
                {
                    return false;
                }

                targetEnumerator.Current.SetNullValue();
                goto TryCompleteList_MoveNext;
            }

            var targetElement = targetEnumerator.Current;
            bool completed;

            switch (elementTypeKind)
            {
                case TypeKind.List:
                    completed = TryCompleteList(
                        element,
                        targetElement,
                        errorTrieForIndex,
                        selection,
                        elementType,
                        depth,
                        resultSelectionSet);
                    break;

                case TypeKind.Scalar:
                    targetElement.SetLeafValue(element);
                    completed = true;
                    break;

                case TypeKind.Enum:
                    CompleteEnumValue(element, targetElement, selection);
                    completed = true;
                    break;

                case TypeKind.Interface or TypeKind.Union:
                    completed = TryCompleteAbstractValue(
                        element,
                        targetElement,
                        errorTrieForIndex,
                        selection,
                        elementType,
                        depth,
                        resultSelectionSet);
                    break;

                default:
                    completed = TryCompleteObjectValue(
                        selection,
                        elementType,
                        element,
                        errorTrieForIndex,
                        depth,
                        targetElement,
                        resultSelectionSet);
                    break;
            }

            if (!completed)
            {
                if (isNonNull)
                {
                    return false;
                }

                targetElement.SetNullValue();
                goto TryCompleteList_MoveNext;
            }

TryCompleteList_MoveNext:
            i++;
        }

        return true;
    }

    private static void CompleteEnumValue(
        SourceResultElement source,
        CompositeResultElement target,
        Selection selection)
    {
        // Reached only for rows flagged as enum values. A string that is an accessible
        // member of the composite enum is written through; anything else (a value unknown
        // to or inaccessible from the composite schema, or a non-string kind) is masked to
        // null so it can never leak past the gateway. The raw UTF-8 payload may contain JSON
        // escape sequences, but GraphQL enum names are [A-Za-z0-9_] only, so an escaped
        // payload cannot match any name and correctly falls through to masking.
        if (selection.NamedType is FusionEnumTypeDefinition enumType
            && source.ValueKind is JsonValueKind.String
            && enumType.Values.ContainsName(source.ValueSpan))
        {
            target.SetLeafValue(source);
        }
        else
        {
            target.SetNullValue();
        }
    }

    private bool TryCompleteObjectValue(
        Selection parentSelection,
        IType type,
        SourceResultElement source,
        ErrorTrie? errorTrie,
        int depth,
        CompositeResultElement target,
        ResultSelectionSet? resultSelectionSet)
    {
        var namedType = type.NamedType();
        var objectType = Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);

        return TryCompleteObjectValue(
            source,
            target,
            errorTrie,
            parentSelection,
            objectType,
            depth,
            resultSelectionSet);
    }

    private bool TryCompleteObjectValue(
        SourceResultElement source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection parentSelection,
        IObjectTypeDefinition objectType,
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        AssertDepthAllowed(ref depth);

        // if the property value is yet undefined we need to initialize it
        // with the current selection set.
        if (target.ValueKind is JsonValueKind.Undefined)
        {
            var objectSelectionSet = parentSelection.GetSelectionSet(objectType)
                ?? throw new InvalidOperationException(
                    "Cannot initialize a result object without a selection set.");
            target.SetObjectValue(objectSelectionSet);
        }

        foreach (var property in source.EnumerateObject())
        {
            if (!target.TryGetProperty(property.NameSpan, out var targetProperty))
            {
                continue;
            }

            var propertyValue = property.Value;
            var propertyValueKind = propertyValue.ValueKind;

            // Fast path: when there are no errors and the source value is a
            // scalar (string, number, bool) we can set it directly without
            // going through the full TryCompleteValue type-dispatch chain.
            if (errorTrie is null && propertyValueKind.IsScalarValue())
            {
                if (propertyValueKind is JsonValueKind.String && targetProperty.IsEnumValue)
                {
                    CompleteEnumValue(propertyValue, targetProperty, targetProperty.AssertSelection());
                    continue;
                }

                targetProperty.SetLeafValue(propertyValue);
                continue;
            }

            var selection = targetProperty.AssertSelection();

            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            var childSet = resultSelectionSet?.TryGetChild(selection.ResponseName, objectType);
            if (!TryCompleteValue(
                    propertyValue,
                    propertyValueKind,
                    targetProperty,
                    errorTrieForResponseName,
                    selection,
                    selection.Type,
                    depth,
                    childSet))
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
        int depth,
        ResultSelectionSet? resultSelectionSet)
        => TryCompleteObjectValue(source, target, errorTrie, selection, GetType(type, source), depth, resultSelectionSet);

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
    public static bool IsScalarValue(this JsonValueKind valueKind)
        => valueKind is JsonValueKind.String or JsonValueKind.Number
            or JsonValueKind.True or JsonValueKind.False;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IType ElementType(this IType type)
        => type switch
        {
            ListType listType => listType.ElementType,
            NonNullType { NullableType: ListType listType } => listType.ElementType,
            _ => throw new ArgumentException($"The type '{type}' is not a list type.", nameof(type))
        };
}
