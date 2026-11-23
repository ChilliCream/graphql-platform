using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Policies;
using HotChocolate.Fusion.Properties;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed class ValueCompletion
{
    private readonly FetchResultStore _store;
    private readonly int _maxDepth;
    private FusionSchemaDefinition _schema = default!;
    private IErrorHandler _errorHandler = default!;
    private ErrorHandlingMode _errorHandlingMode;
    private bool _propagateNullValues;
    private IObjectTypeDefinition? _typeMemoA;
    private IObjectTypeDefinition? _typeMemoB;

    public ValueCompletion(FetchResultStore store, int maxDepth)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _maxDepth = maxDepth;
    }

    /// <summary>
    /// Prepares the value completion for a request or subscription event
    /// that completes values against <paramref name="schema"/>.
    /// </summary>
    public void Initialize(
        FusionSchemaDefinition schema,
        IErrorHandler errorHandler,
        ErrorHandlingMode errorHandlingMode)
    {
        ArgumentNullException.ThrowIfNull(schema);

        _schema = schema;
        _errorHandler = errorHandler;
        _errorHandlingMode = errorHandlingMode;
        _propagateNullValues = errorHandlingMode is not ErrorHandlingMode.Null;
        _typeMemoA = null;
        _typeMemoB = null;
    }

    /// <summary>
    /// Releases the request state so that the value completion can be initialized again.
    /// </summary>
    public void Clean()
    {
        _schema = default!;
        _errorHandler = default!;
        _errorHandlingMode = default;
        _propagateNullValues = false;
        _typeMemoA = null;
        _typeMemoB = null;
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
        var sourceSnapshot = source.CreateSnapshot();

        if (sourceSnapshot.ValueKind is not JsonValueKind.Object)
        {
            var canExecutionContinue =
                BuildResultForInvalidSource(
                    sourceSnapshot,
                    target,
                    resultSelectionSet,
                    errorTrie);

            if (!canExecutionContinue)
            {
                return false;
            }

            return ApplyPocketedErrors(target);
        }

        if (!target.TryGetObjectContext(out var objectContext))
        {
            objectContext = InitializeTargetObject(source, target);
        }
        else if (TryUpgradeOpaqueTarget(objectContext.SelectionSet, target, source))
        {
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

                var propertyValueSnapshot = property.Value.CreateSnapshot();
                var propertyValueKind = propertyValueSnapshot.ValueKind;

                if (errorTrie is null && propertyValueKind.IsScalarValue())
                {
                    if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                    {
                        CompleteEnumValue(
                            propertyValueSnapshot,
                            resultField,
                            Unsafe.As<FusionEnumTypeDefinition>(selection.NamedType));
                        continue;
                    }

                    resultField.SetLeafValue(propertyValueSnapshot);
                    continue;
                }

                ErrorTrie? errorTrieForResponseName = null;
                errorTrie?.TryGetValue(sourceResponseName, out errorTrieForResponseName);

                var childSet = resultSelectionSet.TryGetChild(selection.ResponseName);
                if (!TryCompleteValue(
                        propertyValueSnapshot,
                        resultField,
                        errorTrieForResponseName,
                        selection,
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
            var properties = sourceSnapshot.EnumeratePropertySnapshots();
            var propertySelections = GetPropertySelections(sourceSnapshot, objectContext);
            var propertyIndex = 0;

            while (properties.MoveNext())
            {
                CompositeResultElement resultField;
                Selection? selection;

                if (propertySelections is not null)
                {
                    selection = propertySelections[propertyIndex++];

                    if (selection is null)
                    {
                        continue;
                    }

                    resultField = objectContext.GetProperty(selection);
                }
                else if (!objectContext.TryGetProperty(properties.Name, out resultField, out selection))
                {
                    continue;
                }

                var propertyValueSnapshot = properties.Value;
                var propertyValueKind = propertyValueSnapshot.ValueKind;

                // Fast path: when there are no errors and the source value is a
                // scalar (string, number, bool) we can set it directly without
                // going through the full TryCompleteValue type-dispatch chain.
                if (errorTrie is null && propertyValueKind.IsScalarValue())
                {
                    if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                    {
                        CompleteEnumValue(
                            propertyValueSnapshot,
                            resultField,
                            Unsafe.As<FusionEnumTypeDefinition>(selection.NamedType));
                        continue;
                    }

                    resultField.SetLeafValue(propertyValueSnapshot);
                    continue;
                }

                ErrorTrie? errorTrieForResponseName = null;
                errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

                var childSet = resultSelectionSet.TryGetChild(selection.ResponseName);
                if (!TryCompleteValue(
                        propertyValueSnapshot,
                        resultField,
                        errorTrieForResponseName,
                        selection,
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

        target.InitializeObjectValue(objectSelectionSet, out var objectContext);
        return objectContext;
    }

    /// <summary>
    /// When a covering lookup imports concrete data (carrying a <c>__typename</c>) into an element
    /// that is still interface-typed from an <c>@interfaceObject</c> stand-in, upgrades the element
    /// to its concrete type so the identity-dependent fields have slots to complete into.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the target was upgraded. Any object context acquired for the
    /// target before the call is then stale and must be refetched.
    /// </returns>
    private bool TryUpgradeOpaqueTarget(
        SelectionSet? targetSelectionSet,
        CompositeResultElement target,
        SourceResultElement source)
    {
        if (targetSelectionSet is not { Type.Kind: TypeKind.Interface } interfaceSet
            || interfaceSet.DeclaringSelection is not { } parentSelection)
        {
            return false;
        }

        if (!source.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var typeName)
            || typeName.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        var concreteType = _schema.Types.GetType<IObjectTypeDefinition>(typeName.AssertString());

        if (!parentSelection.IsInternal
            && concreteType is IInaccessibleProvider { IsInaccessible: true })
        {
            _store.Result.RequireNullMarkerFinalization();
        }

        var concreteSelectionSet = parentSelection.GetSelectionSet(concreteType)!;
        _store.Result.UpgradeObject(target, concreteSelectionSet);
        return true;
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

        var errorWithPath = error.WithPath(path);
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
                || fieldResult.Selection is not { IsNonNull: true })
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
                ForwardError(error, parentPath);
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

    public void FinalizeDeniedSelections(
        CompositeResultElement resultData,
        PolicyEvaluationResult policyResult)
        => VisitDeniedSelections(resultData, policyResult);

    /// <summary>
    /// Applies the denial of every denied selection below <paramref name="current"/>.
    /// </summary>
    /// <returns>
    /// <c>false</c>, if the null propagation reached the root and the walk must stop.
    /// </returns>
    private bool VisitDeniedSelections(
        CompositeResultElement current,
        PolicyEvaluationResult policyResult)
    {
        if (current.IsNullOrInvalidated || current.IsNullMarker)
        {
            return true;
        }

        switch (current.ValueKind)
        {
            case JsonValueKind.Object:
                if (current.SelectionSet is null)
                {
                    return true;
                }

                foreach (var property in current.EnumerateObject())
                {
                    var field = property.Value;

                    if (field.IsInternal || property.Selection is not { } selection)
                    {
                        continue;
                    }

                    var canExecutionContinue = selection.HasPolicies && policyResult.IsDenied(selection)
                        ? ApplyDenial(field, selection, policyResult)
                        : VisitDeniedSelections(field, policyResult);

                    if (!canExecutionContinue)
                    {
                        return false;
                    }

                    if (current.IsNullOrInvalidated)
                    {
                        return true;
                    }
                }

                return true;

            case JsonValueKind.Array:
                foreach (var element in current.EnumerateArray())
                {
                    if (!VisitDeniedSelections(element, policyResult))
                    {
                        return false;
                    }

                    if (current.IsNullOrInvalidated)
                    {
                        return true;
                    }
                }

                return true;

            default:
                return true;
        }
    }

    private bool ApplyDenial(
        CompositeResultElement field,
        Selection selection,
        PolicyEvaluationResult policyResult)
    {
        var path = field.Path;
        var error = policyResult.CreateError(selection, path);

        // A propagated null invalidates the parents of the field instead of nulling the field.
        if (_errorHandlingMode is ErrorHandlingMode.Propagate && selection.IsNonNull)
        {
            return error is null || ApplyFieldError(field.Parent, selection, error, path);
        }

        field.SetNullValue();

        return error is null || ApplyFieldError(field, selection, error, path);
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
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        ResultSelectionSet resultSelectionSet,
        ErrorTrie? errorTrie)
    {
        var sourceValueKind = source.ValueKind;

        if (sourceValueKind is JsonValueKind.Null && target.Type?.NamedType().IsSharedValueType() == true)
        {
            if (errorTrie?.FindFirstError() is { } sharedError)
            {
                PocketErrors(target.Path, resultSelectionSet, sharedError);
            }

            return true;
        }

        if (sourceValueKind is JsonValueKind.Null && errorTrie is null or { Count: 0, Error: null })
        {
            return CompleteNullSource(source, target, resultSelectionSet);
        }

        var targetKind = target.ValueKind;
        if (targetKind is JsonValueKind.Undefined && TryInitializeTargetObject(target))
        {
            targetKind = JsonValueKind.Object;
        }

        if (targetKind is JsonValueKind.Object
            && sourceValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            && errorTrie is { Count: > 0 } or { Error: not null }
            && target.GetObjectContext() is { SelectionSet.DeclaringSelection: not null } objectContext)
        {
            return CompleteFailedLookup(errorTrie, target, objectContext, resultSelectionSet);
        }

        var fallbackError = errorTrie?.FindFirstError() ??
            ErrorBuilder.New()
                .SetMessage("Unexpected Execution Error")
                .Build();

        if (targetKind is JsonValueKind.Undefined)
        {
            return BuildErrorResultForUndefinedTarget(
                target,
                resultSelectionSet,
                fallbackError,
                target.CompactPath);
        }

        return BuildErrorResult(target, resultSelectionSet, fallbackError, target.CompactPath);
    }

    private bool CompleteFailedLookup(
        ErrorTrie errorTrie,
        CompositeResultElement target,
        CompositeObjectContext objectContext,
        ResultSelectionSet resultSelectionSet)
    {
        var path = target.Path;
        var lookupError = errorTrie.Error;
        if (errorTrie.Count > 0)
        {
            foreach (var responseName in resultSelectionSet.ResponseNames)
            {
                if (!TryGetFieldErrorTrie(responseName, out var fieldErrorTrie)
                    || !objectContext.TryGetVisibleProperty(responseName, out _, out var selection)
                    || selection.Field.IsIntrospectionField)
                {
                    continue;
                }

                var fieldPath = path.Append(responseName);
                ForwardError(fieldErrorTrie.Error, fieldPath);
                if (fieldErrorTrie.Count > 0)
                {
                    ForwardErrors(fieldErrorTrie, fieldPath, selection.Type, selection);
                }

                if (_errorHandlingMode is ErrorHandlingMode.Propagate && selection.IsNonNull)
                {
                    return !PropagateNullValues(target);
                }

                if (errorTrie.Count == 1)
                {
                    lookupError ??= fieldErrorTrie.Error;
                }
            }
        }

        foreach (var responseName in resultSelectionSet.ResponseNames)
        {
            if (!objectContext.TryGetVisibleProperty(responseName, out var field, out var selection)
                || selection.Field.IsIntrospectionField
                || field.ValueKind is not JsonValueKind.Undefined
                || (errorTrie.Count > 0 && TryGetFieldErrorTrie(responseName, out _)))
            {
                continue;
            }

            lookupError ??= errorTrie.FindFirstError();
            ForwardError(lookupError, path.Append(responseName));
            if (_errorHandlingMode is ErrorHandlingMode.Propagate && selection.IsNonNull)
            {
                return !PropagateNullValues(target);
            }
        }

        return true;

        bool TryGetFieldErrorTrie(string responseName, [NotNullWhen(true)] out ErrorTrie? fieldErrorTrie)
        {
            if (resultSelectionSet.HasSourceResponseNameMappings
                && resultSelectionSet.TryMapResponseName(responseName, out var mapping))
            {
                responseName = mapping.SourceResponseName;
            }

            return errorTrie.TryGetValue(responseName, out fieldErrorTrie);
        }
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
        SourceResultElementSnapshot source,
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
                    fieldResult,
                    errorTrie: null,
                    selection,
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
            ForwardError(error, errorPath.Append(responseName));
        }

        if (!hasResponseNames)
        {
            ForwardError(error, errorPath);
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
        ForwardError(error, path);

        switch (_errorHandlingMode)
        {
            case ErrorHandlingMode.Propagate when selection.IsNonNull:
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

    // TODO: When extracting an error from a path below the current field,
    //       we should try to use the path of the original error if it's
    //       part of what was selected.
    /// <summary>
    /// Completes a source value against the field type of <paramref name="selection"/>.
    /// The type shape is read from facts precomputed on the selection, so a call
    /// site completing a value against any other type must not use this method.
    /// </summary>
    private bool TryCompleteValue(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        var sourceValueKind = source.ValueKind;

        if (sourceValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return TryCompleteNullValue(
                sourceValueKind,
                target,
                errorTrie,
                selection,
                resultSelectionSet);
        }

        // An error on a field that still has a value is forwarded without nulling the value.
        if (errorTrie?.Error is { } fieldError)
        {
            AddFieldError(fieldError, target);
        }

        switch (selection.UnwrappedKind)
        {
            case TypeKind.List:
                // The element shape of the first list level is precomputed on the
                // selection, so it is guaranteed to be present here.
                return TryCompleteList(
                    source,
                    target,
                    errorTrie,
                    selection,
                    selection.ListElementType!,
                    selection.ListElementKind,
                    selection.IsNonNullListElement,
                    depth,
                    resultSelectionSet);

            case TypeKind.Object:
                return TryCompleteObjectValue(
                    selection,
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
                    depth,
                    resultSelectionSet);

            case TypeKind.Scalar:
                target.SetLeafValue(source);
                return true;

            case TypeKind.Enum:
                CompleteEnumValue(source, target, selection);
                return true;

            default:
                throw ThrowHelper.ValueCompletionTypeNotSupported(selection.UnwrappedType);
        }
    }

    /// <summary>
    /// Completes a <c>null</c> or undefined source value against <paramref name="selection"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the execution can continue.
    /// <c>false</c>, if the null must propagate to the parent.
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool TryCompleteNullValue(
        JsonValueKind sourceValueKind,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        ResultSelectionSet? resultSelectionSet)
    {
        if (selection.IsNonNull)
        {
            IError error;
            if (errorTrie?.FindFirstError() is { } errorFromPath)
            {
                var path = target.CompactPath.ToPath(target.Operation);
                error = errorFromPath.WithPath(path);
            }
            else
            {
                var path = target.CompactPath.ToPath(target.Operation);
                error = ErrorBuilder.New()
                    .SetMessage(FusionExecutionResources.ErrorHelper_NonNullViolation)
                    .SetCode(ErrorCodes.Execution.NonNullViolation)
                    .SetPath(path)
                    .Build();
            }

            error = _errorHandler.Handle(error);

            _store.AddError(error);

            return !_propagateNullValues;
        }

        if (sourceValueKind is JsonValueKind.Null && selection.IsValueTypeNamedType)
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
        if (errorTrie?.FindFirstError() is { } nullErrorFromPath)
        {
            ForwardError(nullErrorFromPath, target.Path);
        }
        else
        {
            target.SetNullValue();
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddFieldError(IError fieldError, CompositeResultElement target)
        => ForwardError(fieldError, target.Path);

    /// <summary>
    /// Completes the elements of a nested list. The element shape of nested list
    /// levels is not precomputed on the selection, so it is derived from
    /// <paramref name="type"/> per invocation.
    /// </summary>
    private bool TryCompleteNestedList(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        IType type,
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        var elementType = type.ElementType();
        var elementTypeKind = elementType.Kind;
        var isNonNull = elementTypeKind is TypeKind.NonNull;

        if (isNonNull)
        {
            elementTypeKind = Unsafe.As<IType, NonNullType>(ref elementType).NullableType.Kind;
        }

        return TryCompleteList(
            source,
            target,
            errorTrie,
            selection,
            elementType,
            elementTypeKind,
            isNonNull,
            depth,
            resultSelectionSet);
    }

    private bool TryCompleteList(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        IType elementType,
        TypeKind elementTypeKind,
        bool isNonNull,
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        AssertDepthAllowed(ref depth);

        // A shared list slot may already be populated by a sibling subgraph
        // result. Create the array only on the first write; otherwise reuse it
        // so sibling contributions accumulate through the positional merge below.
        CompositeResultElement.ArrayEnumerator targetEnumerator;

        if (target.ValueKind is JsonValueKind.Undefined)
        {
            targetEnumerator = target.InitializeArrayValue(source.GetArrayLength());
        }
        else if (target.IsNullOrInvalidated && errorTrie is null or { Count: 0 })
        {
            return true;
        }
        else if (target.ValueKind is not JsonValueKind.Array
            || target.GetArrayLength() != source.GetArrayLength())
        {
            return ReportSharedListLengthMismatch(target);
        }
        else
        {
            targetEnumerator = target.EnumerateArray().GetEnumerator();
        }

        var listObjectType = elementTypeKind is TypeKind.Object
            ? (IComplexTypeDefinition)selection.NamedType
            : null;
        SelectionSet? listObjectSelectionSet = null;
        var propagateItemNull = false;
        var i = 0;
        var elements = source.EnumerateElementSnapshots();

        while (elements.MoveNext())
        {
            var success = targetEnumerator.MoveNext();
            Debug.Assert(success, "The lists must have the same size.");

            ErrorTrie? errorTrieForIndex = null;

            if (errorTrie is not null)
            {
                errorTrieForIndex = GetListElementErrorTrie(errorTrie, target, i);
            }

            var elementSnapshot = elements.Current;
            var elementValueKind = elementSnapshot.ValueKind;
            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                // The element might have been nulled due to a down-stream null propagation,
                // so the errors below it are forwarded with their paths.
                if (errorTrieForIndex is { Count: > 0 })
                {
                    ForwardListElementErrors(errorTrieForIndex, target, i, elementType, selection);
                }

                if (isNonNull)
                {
                    if (errorTrieForIndex?.FindFirstError() is null)
                    {
                        var item = targetEnumerator.Current;
                        var error = ErrorHelper.NonNullViolation(item.CompactPath.ToPath(item.Operation));
                        _store.AddError(_errorHandler.Handle(error));
                    }

                    if (_propagateNullValues && !selection.IsLeaf)
                    {
                        return TryCompleteNullContainer(target);
                    }

                    propagateItemNull |= _propagateNullValues;
                }

                targetEnumerator.Current.SetNullValue();
                goto TryCompleteList_MoveNext;
            }

            var targetElement = targetEnumerator.Current;
            bool completed;

            switch (elementTypeKind)
            {
                case TypeKind.List:
                    completed = TryCompleteNestedList(
                        elementSnapshot,
                        targetElement,
                        errorTrieForIndex,
                        selection,
                        elementType,
                        depth,
                        resultSelectionSet);
                    break;

                case TypeKind.Scalar:
                    targetElement.SetLeafValue(elementSnapshot);
                    completed = true;
                    break;

                case TypeKind.Enum:
                    CompleteEnumValue(elementSnapshot, targetElement, selection);
                    completed = true;
                    break;

                case TypeKind.Interface or TypeKind.Union:
                    completed = TryCompleteAbstractValue(
                        elementSnapshot,
                        targetElement,
                        errorTrieForIndex,
                        selection,
                        depth,
                        resultSelectionSet);
                    break;

                default:
                    listObjectSelectionSet ??= selection.GetSelectionSet(listObjectType!);
                    completed = TryCompleteObjectValue(
                        elementSnapshot,
                        targetElement,
                        errorTrieForIndex,
                        selection,
                        listObjectType!,
                        depth,
                        resultSelectionSet,
                        listObjectSelectionSet);
                    break;
            }

            if (!completed)
            {
                if (isNonNull)
                {
                    return TryCompleteNullContainer(target);
                }

                targetElement.SetNullValue();
                goto TryCompleteList_MoveNext;
            }

TryCompleteList_MoveNext:
            i++;
        }

        return !propagateItemNull || TryCompleteNullContainer(target);
    }

    private static bool TryCompleteNullContainer(CompositeResultElement target)
    {
        if (!target.IsNullable)
        {
            return false;
        }

        target.SetNullValue();
        return true;
    }

    /// <summary>
    /// Reports that a shared list slot cannot be merged by position because the
    /// sibling results have different lengths.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the execution can continue.
    /// <c>false</c>, if the null must propagate to the parent.
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ReportSharedListLengthMismatch(CompositeResultElement target)
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

    /// <summary>
    /// Gets the error trie of the list element at <paramref name="index"/> and reports
    /// the error that is attached to the element itself.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorTrie? GetListElementErrorTrie(
        ErrorTrie errorTrie,
        CompositeResultElement target,
        int index)
    {
        errorTrie.TryGetValue(index, out var errorTrieForIndex);

        if (errorTrieForIndex?.Error is { } error)
        {
            ForwardError(error, target.CompactPath.ToPath(target.Operation, index));
        }

        return errorTrieForIndex;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ForwardListElementErrors(
        ErrorTrie errorTrieForIndex,
        CompositeResultElement target,
        int index,
        IType elementType,
        Selection selection)
        => ForwardErrors(
            errorTrieForIndex,
            target.CompactPath.ToPath(target.Operation, index),
            elementType,
            selection);

    /// <summary>
    /// Forwards every error below a null value at <paramref name="path"/>. Each error keeps
    /// its path in client response names down to the deepest segment that maps to a selection.
    /// </summary>
    private void ForwardErrors(
        ErrorTrie errorTrie,
        Path path,
        IType? type,
        Selection selection)
    {
        Stack<ErrorTrieFrame>? stack = null;

        // The direct children are walked without a stack, which is only needed for deeper errors.
        foreach (var (segment, childErrorTrie) in errorTrie)
        {
            var (childPath, childType, childSelection) =
                MapErrorPathSegment(segment, path, type, selection);

            ForwardError(childErrorTrie.Error, childPath);

            if (childErrorTrie.Count > 0)
            {
                stack ??= new Stack<ErrorTrieFrame>();
                stack.Push(new ErrorTrieFrame(childErrorTrie, childPath, childType, childSelection));
                ForwardErrors(stack);
            }
        }
    }

    private void ForwardErrors(Stack<ErrorTrieFrame> stack)
    {
        while (stack.TryPop(out var frame))
        {
            if (!frame.Children.MoveNext())
            {
                continue;
            }

            // The frame goes back with its advanced enumerator so the siblings follow the subtree.
            stack.Push(frame);

            var (segment, childErrorTrie) = frame.Children.Current;
            var (childPath, childType, childSelection) =
                MapErrorPathSegment(segment, frame.Path, frame.Type, frame.Selection);

            ForwardError(childErrorTrie.Error, childPath);

            if (childErrorTrie.Count > 0)
            {
                stack.Push(new ErrorTrieFrame(childErrorTrie, childPath, childType, childSelection));
            }
        }
    }

    private void ForwardError(IError? error, Path path)
    {
        if (error is not null)
        {
            _store.AddError(_errorHandler.Handle(error.WithPath(path)));
        }
    }

    /// <summary>
    /// Maps an error path segment below <paramref name="path"/> to the client path. A segment that
    /// does not map to a client selection keeps <paramref name="path"/> and yields no type.
    /// </summary>
    private static (Path Path, IType? Type, Selection Selection) MapErrorPathSegment(
        object segment,
        Path path,
        IType? type,
        Selection selection)
    {
        if (type?.NullableType() is { } nullableType)
        {
            if (segment is int index && nullableType.Kind is TypeKind.List)
            {
                return (path.Append(index), nullableType.ElementType(), selection);
            }

            if (segment is string responseName
                && nullableType is IObjectTypeDefinition objectType
                && selection.GetSelectionSet(objectType) is { } selectionSet
                && selectionSet.TryGetSelection(responseName, out var fieldSelection)
                && !fieldSelection.IsInternal)
            {
                return (path.Append(fieldSelection.ResponseName), fieldSelection.Type, fieldSelection);
            }
        }

        return (path, null, selection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CompleteEnumValue(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        Selection selection)
    {
        // Reached from dispatch arms guarded only by the type-system enum kind, which
        // does not guarantee the concrete Fusion CLR type, so the pattern match stays
        // as a defensive check that masks any other definition to null. A string that
        // is an accessible member of the composite enum is written through; anything
        // else (a value unknown to or inaccessible from the composite schema, or a
        // non-string kind) is masked to null so it can never leak past the gateway.
        // The raw UTF-8 payload may contain JSON escape sequences, but GraphQL enum
        // names are [A-Za-z0-9_] only, so an escaped payload cannot match any name
        // and correctly falls through to masking.
        if (selection.NamedType is FusionEnumTypeDefinition enumType
            && source.TokenType is JsonTokenType.String
            && enumType.Values.ContainsName(source.ValueSpan))
        {
            target.SetLeafValue(source);
        }
        else
        {
            target.SetNullValue();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CompleteEnumValue(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        FusionEnumTypeDefinition enumType)
    {
        // Reached only from call sites guarded by Selection.IsEnumValue, which
        // guarantees that the selection's named type is a FusionEnumTypeDefinition.
        // The masking semantics match the selection-based overload above.
        if (source.TokenType is JsonTokenType.String
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
        SourceResultElementSnapshot source,
        ErrorTrie? errorTrie,
        int depth,
        CompositeResultElement target,
        ResultSelectionSet? resultSelectionSet)
    {
        var namedType = parentSelection.NamedType;
        var objectType = Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);

        return TryCompleteObjectValue(
            source,
            target,
            errorTrie,
            parentSelection,
            objectType,
            depth,
            resultSelectionSet,
            preparedSelectionSet: null);
    }

    private bool TryCompleteObjectValue(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection parentSelection,
        IComplexTypeDefinition objectType,
        int depth,
        ResultSelectionSet? resultSelectionSet,
        SelectionSet? preparedSelectionSet)
    {
        AssertDepthAllowed(ref depth);

        if (target.IsNullOrInvalidated && errorTrie is null or { Count: 0 })
        {
            return true;
        }

        // if the property value is yet undefined we need to initialize it
        // with the current selection set.
        if (!target.TryGetObjectContext(out var objectContext))
        {
            var objectSelectionSet = preparedSelectionSet
                ?? parentSelection.GetSelectionSet(objectType)
                ?? throw new InvalidOperationException(
                    "Cannot initialize a result object without a selection set.");
            target.InitializeObjectValue(objectSelectionSet, out objectContext);
        }

        if (resultSelectionSet is { HasSourceResponseNameMappings: true })
        {
            return TryCompleteMappedObjectProperties(
                source,
                errorTrie,
                objectType,
                depth,
                resultSelectionSet,
                objectContext)
                || TryCompleteNullContainer(target);
        }

        var properties = source.EnumeratePropertySnapshots();
        var propertySelections = GetPropertySelections(source, objectContext);
        var propertyIndex = 0;

        while (properties.MoveNext())
        {
            CompositeResultElement targetProperty;
            Selection? selection;

            if (propertySelections is not null)
            {
                selection = propertySelections[propertyIndex++];

                if (selection is null)
                {
                    continue;
                }

                targetProperty = objectContext.GetProperty(selection);
            }
            else if (!objectContext.TryGetProperty(properties.Name, out targetProperty, out selection))
            {
                continue;
            }

            var propertyValueSnapshot = properties.Value;
            var propertyValueKind = propertyValueSnapshot.ValueKind;

            if (errorTrie is null)
            {
                // Fast path: when there are no errors and the source value is a
                // scalar (string, number, bool) we can set it directly without
                // going through the full TryCompleteValue type-dispatch chain.
                if (propertyValueKind.IsScalarValue())
                {
                    if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                    {
                        CompleteEnumValue(
                            propertyValueSnapshot,
                            targetProperty,
                            Unsafe.As<FusionEnumTypeDefinition>(selection.NamedType));
                        continue;
                    }

                    targetProperty.SetLeafValue(propertyValueSnapshot);
                    continue;
                }

                if (propertyValueKind is JsonValueKind.Object
                    && selection.UnwrappedKind is TypeKind.Object)
                {
                    var namedType = selection.NamedType;

                    if (!TryCompleteObjectValue(
                        propertyValueSnapshot,
                        targetProperty,
                        errorTrie: null,
                        selection,
                        Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType),
                        depth,
                        resultSelectionSet?.TryGetChild(selection.ResponseName, objectType),
                        preparedSelectionSet: null))
                    {
                        return TryCompleteNullContainer(target);
                    }

                    continue;
                }

                if (propertyValueKind is JsonValueKind.Array
                    && selection.UnwrappedKind is TypeKind.List)
                {
                    if (!TryCompleteList(
                        propertyValueSnapshot,
                        targetProperty,
                        errorTrie: null,
                        selection,
                        selection.ListElementType!,
                        selection.ListElementKind,
                        selection.IsNonNullListElement,
                        depth,
                        resultSelectionSet?.TryGetChild(selection.ResponseName, objectType)))
                    {
                        return TryCompleteNullContainer(target);
                    }

                    continue;
                }
            }

            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            var childSet = resultSelectionSet?.TryGetChild(selection.ResponseName, objectType);
            if (!TryCompleteValue(
                    propertyValueSnapshot,
                    targetProperty,
                    errorTrieForResponseName,
                    selection,
                    depth,
                    childSet))
            {
                return TryCompleteNullContainer(target);
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the selection that every property of <paramref name="source"/> is completed into, in
    /// the order of the properties, with <c>null</c> for a property that is not selected. Returns
    /// <c>null</c> when the properties have to be looked up by their names. The selections are
    /// known when the source object has exactly the fields that its source schema operation
    /// selects, in the order of the operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Selection?[]? GetPropertySelections(
        SourceResultElementSnapshot source,
        CompositeObjectContext objectContext)
        => objectContext.HasSelectionProperties
            ? source.GetPropertySelections(objectContext.SelectionSet!)
            : null;

    /// <summary>
    /// Completes the properties of a source object whose result selection set maps
    /// source response names to different composite response names.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the object was completed.
    /// <c>false</c>, if a non-null violation must propagate to the parent.
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool TryCompleteMappedObjectProperties(
        SourceResultElementSnapshot source,
        ErrorTrie? errorTrie,
        IComplexTypeDefinition objectType,
        int depth,
        ResultSelectionSet resultSelectionSet,
        CompositeObjectContext objectContext)
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

            var propertyValueSnapshot = property.Value.CreateSnapshot();
            var propertyValueKind = propertyValueSnapshot.ValueKind;

            if (errorTrie is null && propertyValueKind.IsScalarValue())
            {
                if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                {
                    CompleteEnumValue(
                        propertyValueSnapshot,
                        targetProperty,
                        Unsafe.As<FusionEnumTypeDefinition>(selection.NamedType));
                    continue;
                }

                targetProperty.SetLeafValue(propertyValueSnapshot);
                continue;
            }

            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(sourceResponseName, out errorTrieForResponseName);

            var childSet = resultSelectionSet.TryGetChild(selection.ResponseName, objectType);
            if (!TryCompleteValue(
                    propertyValueSnapshot,
                    targetProperty,
                    errorTrieForResponseName,
                    selection,
                    depth,
                    childSet))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryCompleteAbstractValue(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        var isOpaque = resultSelectionSet?.ProducesOpaqueElements ?? false;
        var objectType = GetType(selection.NamedType, source.Element, isOpaque);

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
            resultSelectionSet,
            preparedSelectionSet: null);
    }

    /// <summary>
    /// Resolves the runtime object type for a value whose type is not selection
    /// derived, walking the wrappers of <paramref name="type"/> dynamically.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IComplexTypeDefinition GetType(IType type, SourceResultElement data, bool isOpaque)
        => GetType(type.NamedType(), data, isOpaque);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IComplexTypeDefinition GetType(ITypeDefinition namedType, SourceResultElement data, bool isOpaque)
    {
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
        // bytes directly. Beyond 4 candidates the linear scan loses to the dictionary
        // lookup, so larger sets use the memo below.
        if (TryResolveType(typeNameElement, namedType, out var resolvedType))
        {
            return resolvedType;
        }

        // The memo only fronts the large-set fallback below; small implementer sets are
        // fully handled by TryResolveType above.
        var rawTypeName = typeNameElement.AssertUtf8String();

        if (_typeMemoA is { } typeA && Ascii.Equals(rawTypeName, typeA.Name))
        {
            return typeA;
        }

        // Two-slot MRU: a slot B hit swaps the slots so the type just seen is
        // checked first for the next element.
        if (_typeMemoB is { } typeB && Ascii.Equals(rawTypeName, typeB.Name))
        {
            _typeMemoB = _typeMemoA;
            _typeMemoA = typeB;
            return typeB;
        }

        var missedType = _schema.Types.GetType<IObjectTypeDefinition>(rawTypeName);
        _typeMemoB = _typeMemoA;
        _typeMemoA = missedType;
        return missedType;
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
        var typeName = typeNameElement.AssertUtf8String();

        for (var i = 0; i < possibleTypes.Count; i++)
        {
            var possibleType = possibleTypes[i];

            if (Ascii.Equals(typeName, possibleType.Name))
            {
                objectType = possibleType;
                return true;
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
        var typeName = typeNameElement.AssertUtf8String();

        for (var i = 0; i < possibleTypes.Length; i++)
        {
            var possibleType = possibleTypes[i];

            if (Ascii.Equals(typeName, possibleType.Name))
            {
                objectType = possibleType;
                return true;
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
            throw ThrowHelper.ValueCompletionDepthNotAllowed(depth);
        }
    }

    private struct ErrorTrieFrame(
        ErrorTrie errorTrie,
        Path path,
        IType? type,
        Selection selection)
    {
        public Dictionary<object, ErrorTrie>.Enumerator Children = errorTrie.GetEnumerator();

        public readonly Path Path = path;

        public readonly IType? Type = type;

        public readonly Selection Selection = selection;
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
