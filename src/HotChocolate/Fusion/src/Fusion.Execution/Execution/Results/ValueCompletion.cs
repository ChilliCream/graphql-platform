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

        if (errorTrie?.Error is { } lookupError)
        {
            AddFieldError(lookupError, target);
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
                    objectContext.SelectionSet?.Type,
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

                var childSet = resultSelectionSet.TryGetChild(selection.ResponseName, objectContext.SelectionSet?.Type);
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

            while (properties.MoveNext())
            {
                if (!objectContext.TryGetProperty(properties.Name, out var resultField, out var selection))
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

                var childSet = resultSelectionSet.TryGetChild(selection.ResponseName, objectContext.SelectionSet?.Type);
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
        var errorPath = GetErrorPath(path, target.Operation);

        if (!errorPath.IsComplete)
        {
            ReportError(error, errorPath.PublicPrefix);

            return CompleteErrorResult(target, resultSelectionSet);
        }

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

            if (!target.TryGetProperty(responseName, out var fieldResult) || fieldResult.IsInternal)
            {
                continue;
            }

            var selection = fieldResult.AssertSelection();

            if (!ApplyFieldError(fieldResult, selection, error, errorPath.ForField(responseName)))
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
                var errorPath = GetErrorPath(fieldResult);

                return ApplyFieldError(fieldResult, selection, error, errorPath.ResolveDescendant(path));
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

        if (errorTrie is { Count: > 0 } && targetKind is JsonValueKind.Object)
        {
            var completed = target.SelectionSet is not { } fields
                || TryCompleteCollapsedObject(target, errorTrie, fields, resultSelectionSet);

            if (errorTrie.Error is { } directError)
            {
                AddFieldError(directError, target);
            }

            return completed
                ? CompleteErrorResult(target, resultSelectionSet)
                : !PropagateNullValues(target);
        }

        if (errorTrie is { Count: > 0 }
            && TryGetSelectionContext(target, out var selection, out var targetType))
        {
            ForwardSourceErrors(errorTrie, target, targetType, selection, resultSelectionSet);

            return !_propagateNullValues || target.IsNullable || !PropagateNullValues(target);
        }

        var error = errorTrie?.Error;

        if (error is not null && targetKind is JsonValueKind.Object)
        {
            string? onlyField = null;
            foreach (var name in resultSelectionSet.ResponseNames)
            {
                if (!target.TryGetProperty(name, out var field)
                    || field.Selection is not { IsInternal: false } selected
                    || selected.Field.IsIntrospectionField || !_store.IsSelectionActive(selected))
                {
                    continue;
                }

                if (onlyField is not null)
                {
                    onlyField = null;
                    break;
                }

                onlyField = name;
            }

            var path = GetErrorPath(target);
            ReportError(error, path.ForField(onlyField));

            return CompleteErrorResult(target, resultSelectionSet);
        }

        var fallbackError = error ??
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
        var path = GetErrorPath(target);
        var lookupError = errorTrie.Error;

        if (errorTrie.Count > 0)
        {
            foreach (var responseName in resultSelectionSet.ResponseNames)
            {
                if (!TryGetFieldErrorTrie(responseName, out var fieldErrorTrie))
                {
                    continue;
                }

                if (!objectContext.TryGetVisibleProperty(responseName, out _, out var selection)
                    || selection.Field.IsIntrospectionField)
                {
                    continue;
                }

                var fieldPath = path.ForField(responseName);

                if (fieldErrorTrie.Count > 0)
                {
                    var children = resultSelectionSet.TryGetChild(responseName, objectContext.SelectionSet?.Type);
                    var fieldType = path.IsComplete ? selection.Type : null;
                    var context = new ErrorPathContext(fieldPath, fieldType, selection, children, fieldPath);
                    ForwardSourceErrors(fieldErrorTrie, context);
                }
                else if (fieldErrorTrie.Error is { } fieldError)
                {
                    ReportError(fieldError, fieldPath);
                }

                if (_propagateNullValues && selection.IsNonNull)
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

            if (lookupError is not null)
            {
                ReportError(lookupError, path.ForField(responseName));
            }

            if (_propagateNullValues && selection.IsNonNull)
            {
                return !PropagateNullValues(target);
            }
        }

        return true;

        bool TryGetFieldErrorTrie(string responseName, [NotNullWhen(true)] out ErrorTrie? fieldErrorTrie)
        {
            if (!resultSelectionSet.HasSourceResponseNameMappings)
            {
                return errorTrie.TryGetValue(responseName, out fieldErrorTrie);
            }

            foreach (var (segment, child) in errorTrie)
            {
                if (segment is string sourceName
                    && resultSelectionSet.GetResponseName(sourceName, objectContext.SelectionSet?.Type) == responseName)
                {
                    fieldErrorTrie = child;

                    return true;
                }
            }

            fieldErrorTrie = null;

            return false;
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
        var errorPath = GetErrorPath(path, target.Operation);

        if (!errorPath.IsComplete || resultSelectionSet.ResponseNames.IsEmpty)
        {
            ReportError(error, errorPath.PublicPrefix);

            return true;
        }

        foreach (var responseName in resultSelectionSet.ResponseNames)
        {
            ReportError(error, errorPath.ForField(responseName));
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
        if (!selection.IsNonNull && sourceValueKind is JsonValueKind.Null && selection.IsValueTypeNamedType)
        {
            if (errorTrie?.FindFirstError() is { } sharedError && resultSelectionSet is not null)
            {
                PocketErrors(target.Path, resultSelectionSet, sharedError);
            }

            return true;
        }

        if (errorTrie is { Count: > 0 })
        {
            ForwardSourceErrors(errorTrie, target, selection.Type, selection, resultSelectionSet);
        }
        else if (errorTrie?.Error is { } error)
        {
            AddFieldError(error, target);
        }
        else if (selection.IsNonNull)
        {
            ReportNonNullViolation(target);
        }
        else
        {
            target.SetNullValue();
        }

        return !selection.IsNonNull || !_propagateNullValues;
    }

    private void ReportNonNullViolation(CompositeResultElement field)
    {
        var path = GetErrorPath(field).PublicPrefix;
        _store.AddError(_errorHandler.Handle(ErrorHelper.NonNullViolation(path)));
    }

    private void AddFieldError(IError error, CompositeResultElement target)
        => ReportError(error, GetErrorPath(target).PublicPrefix);

    private ClientErrorPath GetErrorPath(CompositeResultElement target)
        => GetErrorPath(target.CompactPath, target.Operation);

    private ClientErrorPath GetErrorPath(CompactPath compactPath, Operation operation)
    {
        var path = Path.Root;
        foreach (var segment in compactPath.Segments)
        {
            if (segment < 0)
            {
                path = path.Append(~segment);
                continue;
            }

            var selection = operation.GetSelectionById(segment);

            if (selection.IsInternal || !_store.IsSelectionActive(selection))
            {
                return new ClientErrorPath(path, IsComplete: false);
            }

            path = path.Append(selection.ResponseName);
        }

        return new ClientErrorPath(path, IsComplete: true);
    }

    private void ReportError(IError error, Path path)
        => _store.AddError(_errorHandler.Handle(error.WithPath(path)));

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
        var i = 0;
        var elements = source.EnumerateElementSnapshots();

        while (elements.MoveNext())
        {
            var success = targetEnumerator.MoveNext();
            Debug.Assert(success, "The lists must have the same size.");

            ErrorTrie? errorTrieForIndex = null;

            errorTrie?.TryGetValue(i, out errorTrieForIndex);

            var targetElement = targetEnumerator.Current;
            var elementSnapshot = elements.Current;
            var elementValueKind = elementSnapshot.ValueKind;
            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (errorTrieForIndex is { Count: > 0 })
                {
                    ForwardSourceErrors(errorTrieForIndex, targetElement, elementType, selection, resultSelectionSet);
                }
                else if (errorTrieForIndex?.Error is { } elementError)
                {
                    AddFieldError(elementError, targetElement);
                }
                else if (isNonNull)
                {
                    ReportNonNullViolation(targetElement);
                }

                if (isNonNull && _propagateNullValues)
                {
                    return TryCompleteNullContainer(target);
                }

                targetElement.SetNullValue();
                goto TryCompleteList_MoveNext;
            }

            if (errorTrieForIndex?.Error is { } itemError)
            {
                AddFieldError(itemError, targetElement);
            }

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

        return true;
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

    private void ForwardSourceErrors(
        ErrorTrie errors,
        CompositeResultElement target,
        IType? type,
        Selection selection,
        ResultSelectionSet? selections)
        => ForwardSourceErrors(errors, GetErrorPath(target).ForSourceErrors(type, selection, selections));

    // A collapsed source subtree keeps the errors reported through the cause at each nullable boundary.
    private void ForwardSourceErrors(ErrorTrie errors, ErrorPathContext context)
    {
        var node = errors;
        var paths = new ErrorPathCandidates(context);
        HashSet<Path>? nulled = null;

        while (node.Count == 1 && node.Error is null)
        {
            var child = node.GetEnumerator();
            child.MoveNext();
            paths = MapErrorPathSegment(paths, child.Current.Key);
            node = child.Current.Value;
        }

        if (node.Count == 0)
        {
            Report(node, paths);

            return;
        }

        var pending = new PriorityQueue<SourceErrorVisit, int>();
        QueueSourceErrors(node, paths);

        while (pending.TryDequeue(out var visit, out _))
        {
            if (nulled is not null && IsBelowNulledPath(visit.Paths.First.Path, nulled))
            {
                continue;
            }

            if (!visit.IsError && visit.Node.Count > 0)
            {
                QueueSourceErrors(visit.Node, visit.Paths);
            }
            else if (!Report(visit.Node, visit.Paths))
            {
                break;
            }
        }

        bool Report(ErrorTrie current, ErrorPathCandidates candidates)
        {
            var field = candidates.First;

            if (current.Error is not { } error)
            {
                return true;
            }

            if (field.Type is not null
                || current == errors
                || ((errors.Error is null || !context.Path.Equals(field.Path)) && !HasReportedErrorAt(field.Path)))
            {
                ReportError(error, field.Path);
            }

            if (candidates.GetPropagationBoundary() is { } boundary)
            {
                if (boundary.Equals(context.Path))
                {
                    return false;
                }

                (nulled ??= []).Add(boundary);
            }

            return true;
        }

        void QueueSourceErrors(ErrorTrie node, ErrorPathCandidates paths)
        {
            if (node.Error is not null)
            {
                pending.Enqueue(new SourceErrorVisit(node, paths, IsError: true), node.ErrorIndex);
            }

            foreach (var (segment, child) in node)
            {
                var mapped = MapErrorPathSegment(paths, segment);
                pending.Enqueue(new SourceErrorVisit(child, mapped, IsError: false), child.FirstErrorIndex);
            }
        }

        static bool IsBelowNulledPath(Path path, HashSet<Path> nulled)
        {
            while (!path.IsRoot)
            {
                if (nulled.Contains(path))
                {
                    return true;
                }

                path = path.Parent;
            }

            return false;
        }
    }

    private ErrorPathCandidates MapErrorPathSegment(
        ErrorPathCandidates parents,
        object segment)
    {
        ErrorPathCandidates mapped = default;
        AddMatches(parents.First);

        if (parents.Others is not null)
        {
            foreach (var parent in parents.Others)
            {
                AddMatches(parent);
            }
        }

        // An unmapped source suffix is reported on its deepest public client selection.
        return mapped.First.Type is null
            ? new ErrorPathCandidates(parents.First with { Type = null })
            : mapped;

        void AddMatches(ErrorPathContext parent)
        {
            var type = parent.Type?.NullableType();

            if (type is ListType list && segment is int index)
            {
                mapped.Add(
                    parent.Child(parent.Path.Append(index), list.ElementType, parent.Selection, parent.Selections));
            }
            else if (type is ITypeDefinition namedType && namedType.IsCompositeType() && segment is string sourceName)
            {
                foreach (var possibleType in _schema.GetPossibleTypes(namedType))
                {
                    var name = parent.Selections?.GetResponseName(sourceName, possibleType) ?? sourceName;

                    if (parent.Selection.GetSelectionSet(possibleType) is { } selectionSet
                        && selectionSet.TryGetSelection(name, out var field)
                        && !field.IsInternal && _store.IsSelectionActive(field))
                    {
                        var children = parent.Selections?.TryGetChild(name, possibleType);
                        mapped.Add(parent.Child(parent.Path.Append(name), field.Type, field, children));
                    }
                }
            }
        }
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
        if (errorTrie?.Error is { } lookupError)
        {
            AddFieldError(lookupError, target);
        }

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

        while (properties.MoveNext())
        {
            if (!objectContext.TryGetProperty(properties.Name, out var targetProperty, out var selection))
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
    /// Completes field errors from a subgraph that returned null for the entire object.
    /// </summary>
    private bool TryCompleteCollapsedObject(
        CompositeResultElement target,
        ErrorTrie errors,
        SelectionSet fields,
        ResultSelectionSet resultSelectionSet)
    {
        foreach (var selection in fields.Selections)
        {
            if (selection.IsInternal || !_store.IsSelectionActive(selection))
            {
                continue;
            }

            var responseName = selection.ResponseName;
            var sourceName = resultSelectionSet.HasSourceResponseNameMappings
                && resultSelectionSet.TryMapResponseName(responseName, fields.Type, out var mapping)
                    ? mapping.SourceResponseName
                    : responseName;

            if (errors.TryGetValue(sourceName, out var fieldErrors)
                && !TryCompleteNullValue(
                    JsonValueKind.Undefined,
                    target.GetPropertyBySelectionId(selection.Id),
                    fieldErrors,
                    selection,
                    resultSelectionSet.TryGetChild(responseName, fields.Type)))
            {
                return false;
            }
        }

        return true;
    }

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
                objectType,
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

    private bool HasReportedErrorAt(Path path)
    {
        if (_store.Errors is { } errors)
        {
            foreach (var error in errors)
            {
                if (path.Equals(error.Path))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private readonly record struct ClientErrorPath(Path PublicPrefix, bool IsComplete)
    {
        public Path ResolveDescendant(Path descendant) => IsComplete ? descendant : PublicPrefix;

        public Path ForField(string? name) => IsComplete && name is not null ? PublicPrefix.Append(name) : PublicPrefix;

        public ErrorPathContext ForSourceErrors(IType? type, Selection selection, ResultSelectionSet? selections)
            => new(PublicPrefix, IsComplete ? type : null, selection, selections, PublicPrefix);
    }

    private readonly record struct ErrorPathContext(
        Path Path,
        IType? Type,
        Selection Selection,
        ResultSelectionSet? Selections,
        Path NullableBoundary)
    {
        public ErrorPathContext Child(Path path, IType type, Selection selection, ResultSelectionSet? selections)
            => new(path, type, selection, selections, type is NonNullType ? NullableBoundary : path);
    }

    private struct ErrorPathCandidates(ErrorPathContext first)
    {
        public ErrorPathContext First = first;
        public List<ErrorPathContext>? Others;

        public readonly Path? GetPropagationBoundary()
        {
            if (First.Type is not NonNullType)
            {
                return null;
            }

            var boundary = First.NullableBoundary;

            if (Others is not null)
            {
                foreach (var candidate in Others)
                {
                    if (candidate.Type is not NonNullType || !First.Path.Equals(candidate.Path))
                    {
                        return null;
                    }

                    if (candidate.NullableBoundary.Length > boundary.Length)
                    {
                        boundary = candidate.NullableBoundary;
                    }
                }
            }

            return boundary;
        }

        public void Add(ErrorPathContext candidate)
        {
            if (First.Type is null)
            {
                First = candidate;
            }
            else if (candidate != First && (Others is null || !Others.Contains(candidate)))
            {
                (Others ??= []).Add(candidate);
            }
        }
    }

    private readonly record struct SourceErrorVisit(ErrorTrie Node, ErrorPathCandidates Paths, bool IsError);
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
