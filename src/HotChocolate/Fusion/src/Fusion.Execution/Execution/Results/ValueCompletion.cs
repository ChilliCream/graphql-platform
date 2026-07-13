using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
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

        CompositeObjectContext objectContext;

        if (target.ValueKind is JsonValueKind.Undefined)
        {
            objectContext = InitializeTargetObject(source, target);
        }
        else
        {
            TryUpgradeOpaqueTarget(target, source);
            objectContext = target.GetObjectContext();
        }

        if (resultSelectionSet.HasSourceResponseNameMappings)
        {
            foreach (var property in source.EnumerateObject())
            {
                CompositeResultElement resultField;
                Selection selection;
                string sourceResponseName;

                if (resultSelectionSet.TryMapSourceResponseName(
                    property,
                    out var responseNameMapping))
                {
                    if (!objectContext.TryGetProperty(
                        responseNameMapping.ResponseNameUtf8,
                        out var mappedResultField,
                        out var mappedSelection))
                    {
                        continue;
                    }

                    resultField = mappedResultField;
                    selection = mappedSelection;
                    sourceResponseName = responseNameMapping.SourceResponseName;
                }
                else
                {
                    if (!objectContext.TryGetProperty(
                        property.NameSpan,
                        out resultField,
                        out selection))
                    {
                        continue;
                    }

                    sourceResponseName = selection.ResponseName;
                }

                var propertyValue = property.Value;
                var propertyValueRow = propertyValue.GetValueRow();
                var propertyValueKind = propertyValueRow.TokenType.ToValueKind();

                if (errorTrie is null && propertyValueKind.IsScalarValue())
                {
                    if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                    {
                        CompleteEnumValue(propertyValue, resultField, selection);
                        continue;
                    }

                    resultField.SetLeafValue(propertyValue, propertyValueRow);
                    continue;
                }

                ErrorTrie? errorTrieForResponseName = null;
                errorTrie?.TryGetValue(sourceResponseName, out errorTrieForResponseName);

                var childSet = resultSelectionSet.TryGetChild(selection.ResponseName);
                if (!TryCompleteValue(
                        propertyValue,
                        propertyValueKind,
                        resultField,
                        errorTrieForResponseName,
                        selection,
                        selection.Type,
                        0,
                        childSet)
                    && _errorHandlingMode is ErrorHandlingMode.Propagate)
                {
                    var didPropagateToRoot = PropagateNullValues(resultField);
                    if (didPropagateToRoot)
                    {
                        return false;
                    }

                    return ApplyPocketedErrors(target);
                }
            }
        }
        else
        {
            foreach (var property in source.EnumerateObject())
            {
                if (!objectContext.TryGetProperty(property.NameSpan, out var resultField, out var selection))
                {
                    continue;
                }

                var propertyValue = property.Value;
                var propertyValueRow = propertyValue.GetValueRow();
                var propertyValueKind = propertyValueRow.TokenType.ToValueKind();

                // Fast path: when there are no errors and the source value is a
                // scalar (string, number, bool) we can set it directly without
                // going through the full TryCompleteValue type-dispatch chain.
                if (errorTrie is null && propertyValueKind.IsScalarValue())
                {
                    if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                    {
                        CompleteEnumValue(propertyValue, resultField, selection);
                        continue;
                    }

                    resultField.SetLeafValue(propertyValue, propertyValueRow);
                    continue;
                }

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
        }

        return ApplyPocketedErrors(target);
    }

    private CompositeObjectContext InitializeTargetObject(
        SourceResultElement source,
        CompositeResultElement target)
    {
        if (!TryGetSelectionContext(target, out var selection, out var type)
            || !type.IsCompositeType())
        {
            throw new InvalidOperationException(
                "Cannot initialize a result object without selection metadata.");
        }

        var objectType = GetType(type, source, isOpaque: false);
        var objectSelectionSet = selection.GetSelectionSet(objectType)!;

        target.SetObjectValue(objectSelectionSet, out var objectContext);
        return objectContext;
    }

    /// <summary>
    /// When a covering lookup imports concrete data (carrying a <c>__typename</c>) into an element
    /// that is still interface-typed from an <c>@interfaceObject</c> stand-in, upgrades the element
    /// to its concrete type so the identity-dependent fields have slots to complete into.
    /// </summary>
    private void TryUpgradeOpaqueTarget(CompositeResultElement target, SourceResultElement source)
    {
        if (target.SelectionSet is not { Type.Kind: TypeKind.Interface } interfaceSet
            || interfaceSet.DeclaringSelection is not { } parentSelection)
        {
            return;
        }

        if (!source.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var typeName)
            || typeName.ValueKind is not JsonValueKind.String)
        {
            return;
        }

        var concreteType = _schema.Types.GetType<IObjectTypeDefinition>(typeName.AssertString());

        if (!parentSelection.IsInternal
            && concreteType is IInaccessibleProvider { IsInaccessible: true })
        {
            _store.Result.RequireNullMarkerFinalization();
        }

        var concreteSelectionSet = parentSelection.GetSelectionSet(concreteType)!;
        _store.Result.UpgradeObject(target, concreteSelectionSet);
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

    public bool BuildErrorResult(Path path, IError error)
    {
        var reachablePath = path;

        while (!reachablePath.IsRoot)
        {
            if (_store.TryGetResult(reachablePath, out var fieldResult)
                && fieldResult.Selection is { } selection)
            {
                return ApplyFieldError(fieldResult, selection, error, path);
            }

            reachablePath = reachablePath.Parent;
        }

        var errorWithPath = ErrorBuilder.FromError(error)
            .SetPath(path)
            .Build();
        _store.AddError(_errorHandler.Handle(errorWithPath));
        return true;
    }

    public bool CompleteErrorResult(
        CompositeResultElement target,
        ResultSelectionSet resultSelectionSet)
    {
        foreach (var responseName in resultSelectionSet.ResponseNames)
        {
            if (target.IsNullOrInvalidated)
            {
                return true;
            }

            if (!target.TryGetProperty(responseName, out var fieldResult)
                || fieldResult.IsInternal
                || fieldResult.Selection is not { Type.Kind: TypeKind.NonNull })
            {
                continue;
            }

            if (_errorHandlingMode is ErrorHandlingMode.Propagate
                && PropagateNullValues(fieldResult))
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

    public void FinalizeInaccessibleRuntimeTypes(CompositeResultElement resultData)
    {
        if (!_store.Result.RequiresNullMarkerFinalization)
        {
            return;
        }

        Visit(resultData, isPublic: true);
        _store.Result.CompleteNullMarkerFinalization();

        static void Visit(CompositeResultElement current, bool isPublic)
        {
            if (!isPublic || current.IsNullOrInvalidated || current.IsNullMarker)
            {
                return;
            }

            switch (current.ValueKind)
            {
                case JsonValueKind.Object:
                    if (isPublic
                        && current.Type?.NamedType().IsAbstractType() == true
                        && current.SelectionSet?.Type is IInaccessibleProvider { IsInaccessible: true })
                    {
                        SetNullMarker(current);
                        return;
                    }

                    foreach (var property in current.EnumerateObject())
                    {
                        Visit(property.Value, !property.Value.IsInternal);
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var element in current.EnumerateArray())
                    {
                        Visit(element, isPublic);
                    }
                    break;
            }
        }
    }

    private static void SetNullMarker(CompositeResultElement result)
    {
        var current = result;

        while (!current.IsRoot && !current.IsNullable)
        {
            current = current.Parent;
        }

        current.SetNullMarker();
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
            || type.NamedType() is not IComplexTypeDefinition complexType)
        {
            return false;
        }

        if (selection.IsLeaf)
        {
            return false;
        }

        target.SetObjectValue(selection.GetSelectionSet(complexType)!);
        return true;
    }

    private static bool TryGetSelectionContext(
        CompositeResultElement target,
        [NotNullWhen(true)] out Selection? selection,
        [NotNullWhen(true)] out IType? type)
    {
        // Fast path: a direct-field target carries its own selection on the
        // preceding property row. Reading that selection once lets us derive
        // the type from it, avoiding a second metadb read via the Type getter.
        selection = target.Selection;

        if (selection is not null)
        {
            type = selection.Type;
            return true;
        }

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
        IComplexTypeDefinition objectType,
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        AssertDepthAllowed(ref depth);

        // if the property value is yet undefined we need to initialize it
        // with the current selection set.
        CompositeObjectContext objectContext;

        if (target.ValueKind is JsonValueKind.Undefined)
        {
            var objectSelectionSet = parentSelection.GetSelectionSet(objectType)
                ?? throw new InvalidOperationException(
                    "Cannot initialize a result object without a selection set.");
            target.SetObjectValue(objectSelectionSet, out objectContext);
        }
        else
        {
            objectContext = target.GetObjectContext();
        }

        if (resultSelectionSet is { HasSourceResponseNameMappings: true })
        {
            foreach (var property in source.EnumerateObject())
            {
                CompositeResultElement targetProperty;
                Selection selection;
                string sourceResponseName;

                if (resultSelectionSet.TryMapSourceResponseName(
                    property,
                    out var responseNameMapping))
                {
                    if (!objectContext.TryGetProperty(
                        responseNameMapping.ResponseNameUtf8,
                        out var mappedTargetProperty,
                        out var mappedSelection))
                    {
                        continue;
                    }

                    targetProperty = mappedTargetProperty;
                    selection = mappedSelection;
                    sourceResponseName = responseNameMapping.SourceResponseName;
                }
                else
                {
                    if (!objectContext.TryGetProperty(
                        property.NameSpan,
                        out targetProperty,
                        out selection))
                    {
                        continue;
                    }

                    sourceResponseName = selection.ResponseName;
                }

                var propertyValue = property.Value;
                var propertyValueRow = propertyValue.GetValueRow();
                var propertyValueKind = propertyValueRow.TokenType.ToValueKind();

                if (errorTrie is null && propertyValueKind.IsScalarValue())
                {
                    if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                    {
                        CompleteEnumValue(propertyValue, targetProperty, selection);
                        continue;
                    }

                    targetProperty.SetLeafValue(propertyValue, propertyValueRow);
                    continue;
                }

                ErrorTrie? errorTrieForResponseName = null;
                errorTrie?.TryGetValue(sourceResponseName, out errorTrieForResponseName);

                var childSet = resultSelectionSet.TryGetChild(selection.ResponseName, objectType);
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
        }
        else
        {
            foreach (var property in source.EnumerateObject())
            {
                if (!objectContext.TryGetProperty(property.NameSpan, out var targetProperty, out var selection))
                {
                    continue;
                }

                var propertyValue = property.Value;
                var propertyValueRow = propertyValue.GetValueRow();
                var propertyValueKind = propertyValueRow.TokenType.ToValueKind();

                // Fast path: when there are no errors and the source value is a
                // scalar (string, number, bool) we can set it directly without
                // going through the full TryCompleteValue type-dispatch chain.
                if (errorTrie is null && propertyValueKind.IsScalarValue())
                {
                    if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                    {
                        CompleteEnumValue(propertyValue, targetProperty, selection);
                        continue;
                    }

                    targetProperty.SetLeafValue(propertyValue, propertyValueRow);
                    continue;
                }

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
    {
        var isOpaque = resultSelectionSet?.ProducesOpaqueElements ?? false;
        var objectType = GetType(type, source, isOpaque);

        if (!selection.IsInternal
            && objectType is IInaccessibleProvider { IsInaccessible: true })
        {
            _store.Result.RequireNullMarkerFinalization();
        }

        return TryCompleteObjectValue(
            source,
            target,
            errorTrie,
            selection,
            objectType,
            depth,
            resultSelectionSet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IComplexTypeDefinition GetType(IType type, SourceResultElement data, bool isOpaque)
    {
        var namedType = type.NamedType();

        if (namedType is IObjectTypeDefinition objectType)
        {
            return objectType;
        }

        // An opaque @interfaceObject value carries no authoritative __typename in the stand-in's
        // result, so it completes interface-typed against the interface's declared fields and only
        // recovers its concrete identity through the covering lookup.
        if (isOpaque)
        {
            return (IComplexTypeDefinition)namedType;
        }

        var typeNameElement = data.GetProperty(IntrospectionFieldNames.TypeNameSpan);

        // Small implementer sets resolve the type by comparing the raw UTF-8 __typename
        // bytes, which is allocation free. Beyond 4 candidates the linear scan loses to
        // the dictionary lookup, so larger sets, escaped values, non-string values, and
        // values that span document chunks use the existing fallback below.
        if (TryResolveType(typeNameElement, namedType, out var resolvedType))
        {
            return resolvedType;
        }

        var typeName = typeNameElement.AssertString();
        return _schema.Types.GetType<IObjectTypeDefinition>(typeName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryResolveType(
        SourceResultElement typeNameElement,
        ITypeDefinition abstractType,
        [NotNullWhen(true)] out FusionObjectTypeDefinition? objectType)
    {
        // Fusion type names are validated GraphQL names, so ASCII equality is equivalent to
        // comparing their UTF-8 encoding with ordinal semantics.
        if (abstractType is FusionUnionTypeDefinition unionType)
        {
            var possibleTypes = unionType.Types;

            if (possibleTypes.Count is > 0
                and <= FusionInterfaceTypeDefinition.MaxTypeNameLookupTypes)
            {
                return TryResolveUnionType(typeNameElement, possibleTypes, out objectType);
            }
        }
        else if (abstractType is FusionInterfaceTypeDefinition interfaceType)
        {
            var possibleTypes = interfaceType.TypeNameLookupTypes;

            if (!possibleTypes.IsDefaultOrEmpty)
            {
                return TryResolveInterfaceType(typeNameElement, possibleTypes, out objectType);
            }
        }

        objectType = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryResolveUnionType(
        SourceResultElement typeNameElement,
        FusionObjectTypeDefinitionCollection possibleTypes,
        [NotNullWhen(true)] out FusionObjectTypeDefinition? objectType)
    {
        if (typeNameElement.TryGetRawStringValue(out var typeName))
        {
            for (var i = 0; i < possibleTypes.Count; i++)
            {
                var possibleType = possibleTypes[i];

                if (Ascii.Equals(typeName, possibleType.Name))
                {
                    objectType = possibleType;
                    return true;
                }
            }
        }

        objectType = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryResolveInterfaceType(
        SourceResultElement typeNameElement,
        ImmutableArray<FusionObjectTypeDefinition> possibleTypes,
        [NotNullWhen(true)] out FusionObjectTypeDefinition? objectType)
    {
        if (typeNameElement.TryGetRawStringValue(out var typeName))
        {
            for (var i = 0; i < possibleTypes.Length; i++)
            {
                var possibleType = possibleTypes[i];

                if (Ascii.Equals(typeName, possibleType.Name))
                {
                    objectType = possibleType;
                    return true;
                }
            }
        }

        objectType = null;
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
