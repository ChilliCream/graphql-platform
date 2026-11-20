using System.Collections.Immutable;
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
    private List<SharedNullError>? _sharedNullErrors;
    private MissingFields _missingFields;
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
        _sharedNullErrors?.Clear();
        HasPendingSharedNulls = false;
        _missingFields = default;
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
        _sharedNullErrors?.Clear();
        HasPendingSharedNulls = false;
        _missingFields = default;
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
            return BuildResultForInvalidSource(sourceSnapshot, target, resultSelectionSet, errorTrie);
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

        var completed = CompleteObjectProperties(
            sourceSnapshot,
            target,
            errorTrie,
            objectContext,
            0,
            resultSelectionSet,
            nextSelection: 0);

        return completed || !PropagateNullValues(target);
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

            if (!target.TryGetProperty(responseName, out var fieldResult))
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

        ReportError(error, path);
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
                || fieldResult.Selection is not { IsNonNull: true } selection
                || !_store.IsSelectionActive(selection)
                || fieldResult.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
            {
                continue;
            }

            if (selection.IsValueTypeNamedType && IsDeferredSharedNull(fieldResult))
            {
                continue;
            }

            if (!PropagatesNulls)
            {
                _missingFields.Cover(fieldResult.CompactPath);
            }
            else if (PropagateNullValues(fieldResult))
            {
                return false;
            }
        }

        return true;
    }

    private bool PropagatesNulls => _errorHandlingMode is not ErrorHandlingMode.Null;

    internal bool HasPendingSharedNulls { get; private set; }

    public void FinalizeResult(PolicyEvaluationResult policyResult)
    {
        var root = _store.Result.Data;
        FinalizeInaccessibleRuntimeTypes(root);

        if (policyResult.RequiresFinalization)
        {
            VisitDeniedSelections(root, policyResult);
        }

        ReportSharedNullErrors(policyResult);
        HashSet<Path?>? reportedPaths = null;

        if (_store.Result.ContainsUndefinedValues && !CompleteMissingFields(root, policyResult, ref reportedPaths))
        {
            root.Invalidate();
        }
    }

    private void ReportSharedNullErrors(PolicyEvaluationResult policyResult)
    {
        var sourceErrorCount = _store.Errors?.Count ?? 0;
        HashSet<Path>? reportedParentErrors = null;

        if (_sharedNullErrors is { Count: > 0 })
        {
            foreach (var (recordedTarget, layoutVersion, selections, directError, hasDescendants) in _sharedNullErrors)
            {
                _missingFields.BeginContribution();
                var target = recordedTarget;

                if (layoutVersion == _store.Result.LayoutVersion
                    || _store.TryGetResult(recordedTarget.CompactPath, out target))
                {
                    CollectMissingSharedFields(target, selections, policyResult);
                }

                if (directError is not null)
                {
                    var path = ResolveDirectErrorPath(recordedTarget, hasDescendants, sourceErrorCount);

                    if (_sharedNullErrors.Count == 1 || (reportedParentErrors ??= []).Add(path))
                    {
                        ReportError(directError, path);
                    }
                }
            }

            _sharedNullErrors.Clear();
        }

        HasPendingSharedNulls = false;
    }

    private bool CompleteMissingFields(
        CompositeResultElement target,
        PolicyEvaluationResult policyResult,
        ref HashSet<Path?>? reportedPaths)
    {
        if (target.IsNullMarker || target.IsNullOrInvalidated)
        {
            return true;
        }

        if (target.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in target.EnumerateArray())
            {
                if (!CompleteMissingFields(item, policyResult, ref reportedPaths))
                {
                    if (!item.IsNullable)
                    {
                        return false;
                    }

                    item.SetNullValue();
                }
            }
        }
        else if (target.ValueKind is JsonValueKind.Object && target.SelectionSet is { } selections)
        {
            foreach (var selection in selections.Selections)
            {
                if (!ShouldCompleteField(selection, policyResult))
                {
                    continue;
                }

                var field = target.GetPropertyBySelectionId(selection.Id);

                if (field.ValueKind is JsonValueKind.Undefined)
                {
                    if (!CompleteMissingField(field, selection, ref reportedPaths))
                    {
                        return false;
                    }
                }
                else if (!selection.IsLeaf && !CompleteMissingFields(field, policyResult, ref reportedPaths))
                {
                    if (selection.IsNonNull)
                    {
                        return false;
                    }

                    field.SetNullValue();
                }
            }
        }

        return true;
    }

    private bool CompleteMissingField(
        CompositeResultElement field,
        Selection selection,
        ref HashSet<Path?>? reportedPaths)
    {
        if (_store.GetRequirementFailure(field) is { } failure)
        {
            if (failure.IsSkipped)
            {
                var path = field.Path;
                reportedPaths ??= _store.Errors?.Select(static e => e.Path).ToHashSet() ?? [];

                if (reportedPaths.Add(path))
                {
                    ReportError(failure.Cause, path);
                }
            }
        }
        else if (selection.IsNonNull)
        {
            var path = field.CompactPath;

            if (!_missingFields.HasSourceError(path))
            {
                ReportNonNullViolation(path);
            }
        }

        if (!selection.IsNonNull)
        {
            return true;
        }

        field.SetNullValue();

        return !PropagatesNulls;
    }

    private bool ShouldCompleteField(Selection selection, PolicyEvaluationResult policyResult)
        => !selection.IsInternal
            && !selection.Field.IsIntrospectionField
            && _store.IsSelectionActive(selection)
            && !(selection.HasPolicies && policyResult.IsDenied(selection));

    private void CollectMissingSharedFields(
        CompositeResultElement target,
        ResultSelectionSet selections,
        PolicyEvaluationResult policyResult)
    {
        if (target.ValueKind is JsonValueKind.Undefined)
        {
            _missingFields.Cover(target.CompactPath);
        }
        else if (target.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in target.EnumerateArray())
            {
                CollectMissingSharedFields(item, selections, policyResult);
            }
        }
        else if (target.ValueKind is JsonValueKind.Object && target.SelectionSet is { } selectionSet)
        {
            foreach (var selection in selectionSet.Selections)
            {
                if (!ShouldCompleteField(selection, policyResult)
                    || !selections.TryGetSelection(selection.ResponseName, selectionSet.Type, out var children))
                {
                    continue;
                }

                var field = target.GetPropertyBySelectionId(selection.Id);

                if (field.ValueKind is JsonValueKind.Undefined)
                {
                    _missingFields.Add(field, selection.IsNonNull);
                }
                else if (children is not null)
                {
                    CollectMissingSharedFields(field, children, policyResult);
                }
            }
        }
    }

    private void FinalizeInaccessibleRuntimeTypes(CompositeResultElement resultData)
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

        while (!current.IsRoot && !current.IsNullable)
        {
            if (current.ValueKind is JsonValueKind.Object)
            {
                current.Invalidate();
            }

            current = current.Parent;
        }

        if (current.IsRoot)
        {
            current.Invalidate();

            return true;
        }

        current.SetNullValue();

        return false;
    }

    private bool BuildResultForInvalidSource(
        in SourceResultElementSnapshot source,
        CompositeResultElement target,
        ResultSelectionSet resultSelectionSet,
        ErrorTrie? errors)
    {
        var sourceValueKind = source.ValueKind;

        if (sourceValueKind is JsonValueKind.Null
            && TryGetSelectionContext(target, out var parentSelection, out var type)
            && type.NamedType().IsSharedValueType())
        {
            RecordSharedNull(target, parentSelection, type, resultSelectionSet, errors);

            return true;
        }

        if (sourceValueKind is JsonValueKind.Null && errors is null or { Count: 0, Error: null })
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
            && errors is { Count: > 0 } or { Error: not null }
            && target.GetObjectContext() is { SelectionSet.DeclaringSelection: not null } objectContext)
        {
            return CompleteFailedLookup(errors, target, objectContext, resultSelectionSet);
        }

        if (errors is { Count: > 0 } && targetKind is JsonValueKind.Object)
        {
            var completed = target.SelectionSet is not { } fields
                || CompleteSparseObject(source, target, errors, fields, resultSelectionSet, depth: 0);

            if (errors.Error is { } directError)
            {
                AddFieldError(directError, target);
            }

            return completed
                ? CompleteErrorResult(target, resultSelectionSet)
                : !PropagateNullValues(target);
        }

        if (errors is { Count: > 0 }
            && TryGetSelectionContext(target, out var selection, out var targetType))
        {
            ForwardSourceErrors(errors, target, targetType, selection, resultSelectionSet);

            return !PropagatesNulls || target.IsNullable || !PropagateNullValues(target);
        }

        var error = errors?.Error;

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

                if (!objectContext.TryGetVisibleProperty(responseName, out var field, out var selection))
                {
                    if (target.TryGetProperty(responseName, out field)
                        && field.Selection is { IsInternal: true } internalSelection
                        && _store.IsSelectionActive(internalSelection))
                    {
                        var children = resultSelectionSet.TryGetChild(responseName, objectContext.SelectionSet?.Type);
                        TryCompleteNullValue(JsonValueKind.Undefined, field, fieldErrorTrie, internalSelection, children);
                    }

                    continue;
                }

                if (selection.Field.IsIntrospectionField)
                {
                    continue;
                }

                if (selection.IsRequirement && fieldErrorTrie.FindFirstError() is { } requirementError)
                {
                    _store.RecordRequirementError(field, requirementError);
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

                if (ShouldPropagateFieldError(field, selection))
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
                if (selection.IsRequirement)
                {
                    _store.RecordRequirementError(field, lookupError);
                }

                ReportError(lookupError, path.ForField(responseName));
            }

            if (ShouldPropagateFieldError(field, selection))
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
        in SourceResultElementSnapshot source,
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
            var childSet = resultSelectionSet.TryGetChild(selection.ResponseName, target.SelectionSet?.Type);

            if (!TryCompleteValue(
                    source,
                    fieldResult,
                    errorTrie: null,
                    selection,
                    0,
                    childSet))
            {
                return !PropagateNullValues(fieldResult);
            }
        }

        return true;
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

        if (!errorPath.IsComplete)
        {
            ReportError(error, errorPath.PublicPrefix);

            return true;
        }

        var hasResponseNames = false;

        foreach (var responseName in resultSelectionSet.ResponseNames)
        {
            hasResponseNames = true;
            ReportError(error, errorPath.ForField(responseName));
        }

        if (!hasResponseNames)
        {
            ReportError(error, errorPath.PublicPrefix);
        }

        return true;
    }

    private bool IsDeferredSharedNull(CompositeResultElement field)
    {
        if (_sharedNullErrors is not null)
        {
            foreach (var contribution in _sharedNullErrors)
            {
                if (contribution.Target.Cursor == field.Cursor)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RecordSharedNull(
        CompositeResultElement target,
        Selection selection,
        IType type,
        ResultSelectionSet? selections,
        ErrorTrie? errors)
    {
        if (errors is null
            && (selections is null
                || (selection.NamedType is IObjectTypeDefinition objectType
                    && selection.GetSelectionSet(objectType) is { HasOnlyNullableLeafFields: true })))
        {
            return;
        }

        var directError = errors?.Error;

        if (errors is not null && (errors.Count > 0 || selections is null))
        {
            var errorPath = GetErrorPath(target);

            if (errors.Count > 0)
            {
                directError = ForwardDescendantErrors(errors, errorPath.ForSourceErrors(type, selection, selections),
                    includeDirectError: true);
            }

            if (selections is null && directError is not null)
            {
                ReportError(directError, errorPath.PublicPrefix);
            }
        }

        if (selections is not null)
        {
            HasPendingSharedNulls = true;

            if (errors is not null)
            {
                (_sharedNullErrors ??= []).Add(
                    new SharedNullError(target, _store.Result.LayoutVersion, selections, directError, errors.Count > 0));
            }
        }
    }

    private bool ApplyFieldError(
        CompositeResultElement fieldResult,
        Selection selection,
        IError error,
        Path path)
    {
        if (selection.IsRequirement)
        {
            _store.RecordRequirementError(fieldResult, error);

            if (selection.IsInternal)
            {
                return true;
            }
        }

        ReportError(error, path);

        return !ShouldPropagateFieldError(fieldResult, selection) || !PropagateNullValues(fieldResult);
    }

    private bool ShouldPropagateFieldError(CompositeResultElement field, Selection selection)
    {
        if (selection.IsNonNull)
        {
            if (PropagatesNulls)
            {
                return true;
            }

            if (field.ValueKind is JsonValueKind.Undefined)
            {
                _missingFields.Cover(field.CompactPath);
            }
        }

        return false;
    }

    /// <summary>
    /// Completes a source value against the field type of <paramref name="selection"/>.
    /// The type shape is read from facts precomputed on the selection, so a call
    /// site completing a value against any other type must not use this method.
    /// </summary>
    private bool TryCompleteValue(
        in SourceResultElementSnapshot source,
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
                return TryCompleteEnumValue(source, target, selection, selection.IsNonNull, errorTrie);

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
        if (errorTrie is not null && selection.IsRequirement && errorTrie.FindFirstError() is { } requirementError)
        {
            _store.RecordRequirementError(target, requirementError);

            if (selection.IsInternal)
            {
                return true;
            }
        }

        if (selection.IsValueTypeNamedType
            && (!selection.IsNonNull || errorTrie is { Count: > 0 })
            && (sourceValueKind is JsonValueKind.Null || errorTrie is not null))
        {
            RecordSharedNull(target, selection, selection.Type, resultSelectionSet, errorTrie);

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
        else if (selection.IsNonNull && errorTrie is not { Count: > 0 })
        {
            ReportNonNullViolation(target.CompactPath);
        }

        // A failed field stays unresolved for dependent lookups. An explicit, successful null is bindable.
        if (errorTrie is { Error: not null } or { Count: > 0 })
        {
            if (selection.IsNonNull)
            {
                if (PropagatesNulls)
                {
                    return false;
                }

                _missingFields.Cover(target.CompactPath);
            }

            return true;
        }

        if (selection.IsInternal && sourceValueKind is JsonValueKind.Undefined)
        {
            return true;
        }

        if (!selection.IsNonNull || !PropagatesNulls)
        {
            target.SetNullValue();

            return true;
        }

        return false;
    }

    private void ReportNonNullViolation(CompactPath path)
    {
        var errorPath = GetErrorPath(path, _store.Result.GetOperation()).PublicPrefix;
        _store.AddError(_errorHandler.Handle(ErrorHelper.NonNullViolation(errorPath)));
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
        in SourceResultElementSnapshot source,
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
        in SourceResultElementSnapshot source,
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

        if (source.TokenType.IsScalarValue() && selection.IsEnumValue)
        {
            return CompleteInvalidValue(target, !target.IsNullable, errorTrie);
        }

        // A shared list slot may already be populated by a sibling subgraph
        // result. Create the array only on the first write; otherwise reuse it
        // so sibling contributions accumulate through the positional merge below.
        CompositeResultElement.ArrayEnumerator targetEnumerator;

        var targetKind = target.ValueKind;
        var isNewList = targetKind is JsonValueKind.Undefined;

        if (isNewList)
        {
            targetEnumerator = target.InitializeArrayValue(source.GetArrayLength());
        }
        else if (target.IsNullOrInvalidated)
        {
            if (errorTrie is { Count: > 0 })
            {
                ForwardDescendantErrors(errorTrie, GetErrorPath(target).ForSourceErrors(target.Type, selection, resultSelectionSet),
                    includeDirectError: false);
            }

            return true;
        }
        else if (targetKind is JsonValueKind.Array && target.GetArrayLength() == source.GetArrayLength())
        {
            targetEnumerator = target.EnumerateArray().GetEnumerator();
        }
        else
        {
            return ReportSharedListLengthMismatch(target);
        }

        if (elementTypeKind is TypeKind.Scalar && errorTrie is null)
        {
            return CompleteScalarList(source, targetEnumerator, isNonNull);
        }

        var listObjectType = elementTypeKind is TypeKind.Object
            ? (IComplexTypeDefinition)selection.NamedType
            : null;
        var listObjectSelectionSet = listObjectType is null ? null : selection.GetSelectionSet(listObjectType);
        var firstSelection = 0;

        if (listObjectSelectionSet is not null
            && resultSelectionSet is { Selections: [var first, ..] }
            && listObjectSelectionSet.TryGetSelection(first.ResponseName, out var field))
        {
            firstSelection = field.Id - listObjectSelectionSet.Id - 1;
        }

        var i = 0;
        var elements = source.EnumerateElementSnapshots();

        while (elements.MoveNext())
        {
            var targetElement = targetEnumerator.NextItem();

            ErrorTrie? errorTrieForIndex = null;
            errorTrie?.TryGetValue(i, out errorTrieForIndex);

            var elementSnapshot = elements.Current;
            var elementValueKind = elementSnapshot.ValueKind;
            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (selection.IsValueTypeNamedType
                    && (!isNonNull || errorTrieForIndex is { Count: > 0 }))
                {
                    RecordSharedNull(targetElement, selection, elementType, resultSelectionSet, errorTrieForIndex);
                    goto TryCompleteList_MoveNext;
                }

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
                    ReportNonNullViolation(targetElement.CompactPath);
                }

                if (isNonNull && PropagatesNulls)
                {
                    return false;
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
                    completed = TryCompleteEnumValue(elementSnapshot, targetElement, selection, isNonNull, errorTrieForIndex);
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
                    completed = TryCompleteObjectValue(
                        elementSnapshot,
                        targetElement,
                        errorTrieForIndex,
                        selection,
                        listObjectType!,
                        depth,
                        resultSelectionSet,
                        listObjectSelectionSet!,
                        isNewList,
                        firstSelection);
                    break;
            }

            if (!completed)
            {
                if (isNonNull)
                {
                    return false;
                }

                targetElement.SetNullValue();
            }

TryCompleteList_MoveNext:
            i++;
        }

        return true;
    }

    private bool CompleteScalarList(
        in SourceResultElementSnapshot source,
        CompositeResultElement.ArrayEnumerator targets,
        bool isNonNull)
    {
        var elements = source.EnumerateElementSnapshots();
        while (elements.MoveNext())
        {
            var element = elements.Current;
            var target = targets.NextItem();

            if (element.TokenType is JsonTokenType.Null or JsonTokenType.None)
            {
                if (isNonNull)
                {
                    ReportNonNullViolation(target.CompactPath);

                    if (PropagatesNulls)
                    {
                        return false;
                    }
                }

                target.SetNullValue();
            }
            else
            {
                target.SetLeafValue(element);
            }
        }

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

        return !PropagatesNulls;
    }

    private void ForwardSourceErrors(
        ErrorTrie errors,
        CompositeResultElement target,
        IType? type,
        Selection selection,
        ResultSelectionSet? selections)
        => ForwardSourceErrors(errors, GetErrorPath(target).ForSourceErrors(type, selection, selections));

    private void ForwardSourceErrors(ErrorTrie errors, ErrorPathContext context)
    {
        if (ForwardDescendantErrors(errors, context, includeDirectError: true) is { } directError)
        {
            ReportError(directError, context.Path);
        }
    }

    // Reports descendants through the error that nulls each nullable ancestor.
    // Returns the direct error if it was reached in the source's reported order.
    private IError? ForwardDescendantErrors(ErrorTrie errors, ErrorPathContext context, bool includeDirectError)
    {
        var node = errors;
        var paths = new ErrorPathCandidates(context);
        HashSet<Path>? nulled = null;
        IError? directError = null;

        while (node.Count == 1 && (!includeDirectError || node.Error is null))
        {
            var child = node.GetEnumerator();
            child.MoveNext();
            paths = MapErrorPathSegment(paths, child.Current.Key);
            node = child.Current.Value;
            includeDirectError = true;
        }

        if (node.Count == 0)
        {
            if (includeDirectError)
            {
                Report(node, paths);
            }

            return directError;
        }

        var pending = new PriorityQueue<SourceErrorVisit, int>();
        QueueSourceErrors(node, paths, includeDirectError);

        while (pending.TryDequeue(out var visit, out _))
        {
            if (nulled is not null && IsBelowNulledPath(visit.Paths.First.Path, nulled))
            {
                continue;
            }

            if (!visit.IsError && visit.Node.Count > 0)
            {
                QueueSourceErrors(visit.Node, visit.Paths, includeError: true);
            }
            else if (!Report(visit.Node, visit.Paths))
            {
                break;
            }
        }

        return directError;

        bool Report(ErrorTrie current, ErrorPathCandidates candidates)
        {
            var field = candidates.First;

            if (current.Error is not { } error)
            {
                return true;
            }

            if (current == errors)
            {
                directError = error;
            }
            else if (field.Type is not null
                || ((errors.Error is null || !context.Path.Equals(field.Path))
                    && !HasReportedErrorAt(field.Path, _store.Errors?.Count ?? 0)))
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

        void QueueSourceErrors(
            ErrorTrie node,
            ErrorPathCandidates paths,
            bool includeError)
        {
            if (includeError && node.Error is not null)
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

    private bool TryCompleteEnumValue(
        in SourceResultElementSnapshot source,
        CompositeResultElement target,
        Selection selection,
        bool isNonNull,
        ErrorTrie? errors)
    {
        if (source.TokenType is JsonTokenType.String
            && selection.NamedType is FusionEnumTypeDefinition enumType
            && enumType.Values.ContainsName(source.Utf8Text))
        {
            target.SetLeafValue(source);

            return true;
        }

        return CompleteInvalidValue(target, isNonNull, errors);
    }

    private bool CompleteInvalidValue(CompositeResultElement target, bool isNonNull, ErrorTrie? errors)
    {
        if (isNonNull && errors?.Error is null)
        {
            ReportNonNullViolation(target.CompactPath);
        }

        target.SetNullValue();

        return !isNonNull || !PropagatesNulls;
    }

    private bool TryCompleteObjectValue(
        Selection parentSelection,
        in SourceResultElementSnapshot source,
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
            parentSelection.GetSelectionSet(objectType)!,
            isNew: false,
            firstSelection: 0);
    }

    private bool TryCompleteObjectValue(
        in SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection parentSelection,
        IComplexTypeDefinition objectType,
        int depth,
        ResultSelectionSet? resultSelectionSet,
        SelectionSet selectionSet,
        bool isNew,
        int firstSelection)
    {
        AssertDepthAllowed(ref depth);
        CompositeObjectContext objectContext;

        if (isNew)
        {
            target.InitializeObjectValue(selectionSet, out objectContext);
        }
        else if (!target.TryGetOrInitializeObject(selectionSet, out objectContext))
        {
            if (errorTrie is { Count: > 0 })
            {
                ForwardDescendantErrors(errorTrie, GetErrorPath(target).ForSourceErrors(objectType, parentSelection, resultSelectionSet),
                    includeDirectError: false);
            }

            return true;
        }

        return CompleteObjectProperties(source, target, errorTrie, objectContext, depth, resultSelectionSet, firstSelection);
    }

    private bool CompleteObjectProperties(
        in SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        in CompositeObjectContext objectContext,
        int depth,
        ResultSelectionSet? resultSelectionSet,
        int nextSelection)
    {
        if (errorTrie is not null || resultSelectionSet is { HasSourceResponseNameMappings: true })
        {
            return TryCompleteObjectPropertiesWithErrorsOrMappings(source, target, errorTrie, depth, resultSelectionSet, objectContext);
        }

        var properties = source.EnumeratePropertySnapshots();

        while (properties.MoveNext())
        {
            if (!objectContext.TryGetProperty(properties.Name, ref nextSelection, out var field, out var selection))
            {
                continue;
            }

            var value = properties.Value;

            if (value.TokenType.IsScalarValue() && !selection.IsEnumValue)
            {
                field.SetLeafValue(value);
                continue;
            }

            var children = resultSelectionSet?.TryGetChild(selection.ResponseName, objectContext.SelectionSet?.Type);

            if (!TryCompleteValue(value, field, null, selection, depth, children)
                && !AbsorbFieldNull(field, selection))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Completes the properties of a source object with reported errors or mapped response names.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the object was completed.
    /// <c>false</c>, if a non-null violation must propagate to the parent.
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool TryCompleteObjectPropertiesWithErrorsOrMappings(
        in SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        int depth,
        ResultSelectionSet? resultSelectionSet,
        in CompositeObjectContext objectContext)
    {
        if (errorTrie is { Count: > 0 } && objectContext.SelectionSet is { } selectionSet)
        {
            foreach (var segment in errorTrie.Keys)
            {
                if (segment is string name && !source.TryGetProperty(name, out _))
                {
                    return CompleteSparseObject(source, target, errorTrie, selectionSet, resultSelectionSet, depth);
                }
            }
        }

        var objectType = objectContext.SelectionSet?.Type;
        foreach (var property in source.EnumerateObject())
        {
            var responseName = property.NameSpan;
            string? sourceAlias = null;

            if (resultSelectionSet is { HasSourceResponseNameMappings: true }
                && resultSelectionSet.TryMapSourceResponseName(property, objectType, out var mapping))
            {
                responseName = mapping.ResponseNameUtf8;
                sourceAlias = mapping.SourceResponseName;
            }

            if (!objectContext.TryGetProperty(responseName, out var targetProperty, out var selection))
            {
                continue;
            }

            var propertyValueSnapshot = property.Value.CreateSnapshot();
            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(sourceAlias ?? selection.ResponseName, out errorTrieForResponseName);

            var childSet = resultSelectionSet?.TryGetChild(selection.ResponseName, objectType);

            if (!TryCompleteValue(
                    propertyValueSnapshot,
                    targetProperty,
                    errorTrieForResponseName,
                    selection,
                    depth,
                    childSet)
                && !AbsorbFieldNull(targetProperty, selection))
            {
                return false;
            }
        }

        return true;
    }

    // An omitted field with an error occupies its original position in the selection set.
    private bool CompleteSparseObject(
        in SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie errors,
        SelectionSet fields,
        ResultSelectionSet? selections,
        int depth)
    {
        foreach (var selection in fields.Selections)
        {
            if (!_store.IsSelectionActive(selection))
            {
                continue;
            }

            var name = selection.ResponseName;
            var sourceName = selections is { HasSourceResponseNameMappings: true }
                && selections.TryMapResponseName(name, fields.Type, out var mapping)
                    ? mapping.SourceResponseName
                    : name;
            var value = default(SourceResultElement);
            var present = source.ValueKind is JsonValueKind.Object && source.TryGetProperty(sourceName, out value);
            errors.TryGetValue(sourceName, out var fieldErrors);

            if (!present && fieldErrors is null)
            {
                continue;
            }

            var field = target.GetPropertyBySelectionId(selection.Id);
            var children = selections?.TryGetChild(name, fields.Type);

            if (!TryCompleteValue(present ? value.CreateSnapshot() : default, field, fieldErrors, selection, depth, children)
                && !AbsorbFieldNull(field, selection))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AbsorbFieldNull(CompositeResultElement field, Selection selection)
    {
        if (selection.IsNonNull && !selection.IsInternal)
        {
            return false;
        }

        field.SetNullValue();

        return true;
    }

    private bool TryCompleteAbstractValue(
        in SourceResultElementSnapshot source,
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
            selection.GetSelectionSet(objectType)!,
            isNew: false,
            firstSelection: 0);
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

        // Small implementer sets resolve the type by comparing the decoded UTF-8 __typename
        // bytes directly. Beyond 4 candidates the linear scan loses to the dictionary
        // lookup, so larger sets use the memo below.
        if (TryResolveType(typeNameElement, namedType, out var resolvedType))
        {
            return resolvedType;
        }

        // The memo only fronts the large-set fallback below; small implementer sets are
        // fully handled by TryResolveType above.
        var typeName = typeNameElement.CreateSnapshot().Utf8Text;

        if (_typeMemoA is { } typeA && Ascii.Equals(typeName, typeA.Name))
        {
            return typeA;
        }

        // Two-slot MRU: a slot B hit swaps the slots so the type just seen is
        // checked first for the next element.
        if (_typeMemoB is { } typeB && Ascii.Equals(typeName, typeB.Name))
        {
            _typeMemoB = _typeMemoA;
            _typeMemoA = typeB;
            return typeB;
        }

        var missedType = _schema.Types.GetType<IObjectTypeDefinition>(typeName);
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
        var typeName = typeNameElement.CreateSnapshot().Utf8Text;

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
        var typeName = typeNameElement.CreateSnapshot().Utf8Text;

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

    private readonly record struct SharedNullError(
        CompositeResultElement Target,
        int LayoutVersion,
        ResultSelectionSet Selections,
        IError? DirectError,
        bool HasDescendants);

    private struct MissingFields
    {
        private HashSet<CompactPath>? _covered;
        private CompactPath _first;
        private int _count;

        public void BeginContribution() => _count = 0;

        public void Cover(CompactPath path) => (_covered ??= []).Add(path);

        public readonly bool HasSourceError(CompactPath path) => _covered?.Contains(path) == true;

        public void Add(CompositeResultElement field, bool isRequired)
        {
            var path = field.CompactPath;

            if (_count++ == 0)
            {
                _first = path;
            }

            if (isRequired)
            {
                Cover(path);
            }
        }

        public readonly CompactPath? OnlyMissingField => _count == 1 ? _first : null;
    }

    private Path ResolveDirectErrorPath(CompositeResultElement target, bool hasDescendants, int sourceErrorCount)
    {
        if (!hasDescendants && _missingFields.OnlyMissingField is { } onlyField)
        {
            var child = GetErrorPath(onlyField, target.Operation).PublicPrefix;

            if (!HasReportedErrorAt(child, sourceErrorCount))
            {
                return child;
            }
        }

        return GetErrorPath(target).PublicPrefix;
    }

    private bool HasReportedErrorAt(Path path, int count)
    {
        if (_store.Errors is { } errors)
        {
            for (var i = 0; i < count; i++)
            {
                if (path.Equals(errors[i].Path))
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
    public static bool IsScalarValue(this JsonTokenType tokenType)
        => tokenType is JsonTokenType.String or JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IType ElementType(this IType type)
        => type switch
        {
            ListType listType => listType.ElementType,
            NonNullType { NullableType: ListType listType } => listType.ElementType,
            _ => throw new ArgumentException($"The type '{type}' is not a list type.", nameof(type))
        };
}
