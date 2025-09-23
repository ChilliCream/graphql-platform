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
    private readonly ISchemaDefinition _schema;
    private readonly IErrorHandler _errorHandler;
    private readonly ErrorHandlingMode _errorHandlingMode;
    private readonly int _maxDepth;
    private readonly ulong _includeFlags;
    private readonly List<IError> _errors;

    public ValueCompletion(
        ISchemaDefinition schema,
        IErrorHandler errorHandler,
        ErrorHandlingMode errorHandlingMode,
        int maxDepth,
        ulong includeFlags,
        List<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(errors);

        _schema = schema;
        _errorHandler = errorHandler;
        _errorHandlingMode = errorHandlingMode;
        _maxDepth = maxDepth;
        _includeFlags = includeFlags;
        _errors = errors;
    }

    /// <summary>
    /// Tries to complete the <paramref name="selectionSet"/> from the
    /// <paramref name="data"/>, checking for errors on the <paramref name="errorTrie"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the execution can continue.
    /// <c>false</c>, if the execution needs to be halted.
    /// </returns>
    public bool BuildResult(
        SourceResultElement data,
        ErrorTrie? errorTrie,
        ReadOnlySpan<string> responseNames,
        CompositeResultElement result)
    {
        var selectionSet = result.GetRequiredSelectionSet();

        if (data is not { ValueKind: JsonValueKind.Object })
        {
            var error = errorTrie?.FindFirstError() ??
                ErrorBuilder.New()
                    .SetMessage("Unexpected Execution Error")
                    .Build();

            return BuildErrorResult(result, responseNames, error, result.Path);
        }

        foreach (var property in data.EnumerateObject())
        {
            // TODO : we need to optimize the lookup performance of the result object
            // at the moment its close to what the JSON Document would do,
            // but since the result object has a selection set and each property
            // is associated with a selection index we can get the lookup performance close
            // the the one of a frozen dictionary.
            if (!result.TryGetProperty(property.NameSpan, out var resultField))
            {
                continue;
            }

            var selection = resultField.GetRequiredSelection();
            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            if (!TryCompleteValue(selection, selection.Type, property.Value, errorTrieForResponseName, 0, resultField))
            {
                switch (_errorHandlingMode)
                {
                    case ErrorHandlingMode.Propagate:
                        var didPropagateToRoot = PropagateNullValues(result);
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
        CompositeResultElement element,
        ReadOnlySpan<string> responseNames,
        IError error,
        Path path)
    {
        foreach (var responseName in responseNames)
        {
            var fieldResult = objectResult[responseName];

            if (fieldResult.Selection.IsInternal || !fieldResult.Selection.IsIncluded(_includeFlags))
            {
                continue;
            }

            var errorWithPath = ErrorBuilder.FromError(error)
                .SetPath(path.Append(responseName))
                .AddLocation(fieldResult.Selection.SyntaxNodes[0].Node)
                .Build();
            errorWithPath = _errorHandler.Handle(errorWithPath);

            _errors.Add(errorWithPath);

            if (_errorHandlingMode is ErrorHandlingMode.Halt)
            {
                return false;
            }

            if (_errorHandlingMode is ErrorHandlingMode.Propagate && fieldResult.Selection.Type.IsNonNullType())
            {
                var didPropagateToRoot = PropagateNullValues(objectResult);

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
    private static bool PropagateNullValues(ResultData result)
    {
        if (result.IsInvalidated)
        {
            return result.Parent is null;
        }

        result.IsInvalidated = true;

        while (result.Parent is not null)
        {
            var index = result.ParentIndex;
            var parent = result.Parent;

            if (parent.IsInvalidated || parent.TrySetValueNull(index))
            {
                return false;
            }

            parent.IsInvalidated = true;

            result = parent;
        }

        return true;
    }

    // TODO: When extracting an error from a path below the current field,
    //       we should try to use the path of the original error if it's
    //       part of what was selected.
    private bool TryCompleteValue(
        Selection selection,
        IType type,
        SourceResultElement source,
        ErrorTrie? errorTrie,
        int depth,
        CompositeResultElement target)
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
                _errors.Add(error);

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
                _errors.Add(errorWithPath);

                if (_errorHandlingMode is ErrorHandlingMode.Halt)
                {
                    return false;
                }
            }

            return true;
        }

        if (type.Kind is TypeKind.List)
        {
            return TryCompleteList(selection, type, source, errorTrie, depth, target);
        }

        if (type.Kind is TypeKind.Object)
        {
            return TryCompleteObjectValue(selection, type, source, errorTrie, depth, target);
        }

        if (type.Kind is TypeKind.Interface or TypeKind.Union)
        {
            return TryCompleteAbstractValue(selection, type, source, errorTrie, depth, target);
        }

        if (type.Kind is TypeKind.Scalar or TypeKind.Enum)
        {
            target.SetNextValue(source);
            return true;
        }

        throw new NotSupportedException($"The type {type} is not supported.");
    }

    private bool TryCompleteList(
        Selection selection,
        IType type,
        SourceResultElement source,
        ErrorTrie? errorTrie,
        int depth,
        CompositeResultElement parent)
    {
        AssertDepthAllowed(ref depth);

        var elementType = type.ElementType();
        var isNullable = elementType.IsNullableType();
        var isLeaf = elementType.IsLeafType();
        var isNested = elementType.IsListType();

        ListResult listResult = isNested
            ? _resultPoolSession.RentNestedListResult()
            : isLeaf
                ? _resultPoolSession.RentLeafListResult()
                : _resultPoolSession.RentObjectListResult();
        listResult.Initialize(type);

        var i = -1;
        foreach (var item in source.EnumerateArray())
        {
            i++;

            ErrorTrie? errorTrieForIndex = null;
            errorTrie?.TryGetValue(i, out errorTrieForIndex);

            if (errorTrieForIndex?.Error is { } error)
            {
                var errorWithPath = ErrorBuilder.FromError(error)
                    .SetPath(parent.Path.Append(i))
                    .AddLocation(selection.SyntaxNodes[0].Node)
                    .Build();
                errorWithPath = _errorHandler.Handle(errorWithPath);
                _errors.Add(errorWithPath);

                if (_errorHandlingMode is ErrorHandlingMode.Halt)
                {
                    return false;
                }
            }

            if (item.IsNullOrUndefined())
            {
                if (!isNullable && _errorHandlingMode is ErrorHandlingMode.Propagate or ErrorHandlingMode.Halt)
                {
                    return false;
                }

                listResult.SetNextValueNull();

                continue;
            }

            if (!HandleElement(item, errorTrieForIndex))
            {
                if (!isNullable)
                {
                    return false;
                }

                listResult.SetNextValueNull();
            }
        }

        parent.SetNextValue(listResult);
        return true;

        bool HandleElement(in JsonElement item, ErrorTrie? errorTrieForIndex)
        {
            if (isNested)
            {
                return TryCompleteList(selection, elementType, item, errorTrieForIndex, depth, listResult);
            }
            else if (isLeaf)
            {
                listResult.SetNextValue(item);
                return true;
            }
            else if (elementType.IsAbstractType())
            {
                return TryCompleteAbstractValue(selection, elementType, item, errorTrieForIndex, depth, listResult);
            }
            else
            {
                return TryCompleteObjectValue(selection, elementType, item, errorTrieForIndex, depth, listResult);
            }
        }
    }

    private bool TryCompleteObjectValue(
        Selection parentSelection,
        IType type,
        JsonElement data,
        ErrorTrie? errorTrie,
        int depth,
        ResultData parent)
    {
        var namedType = type.NamedType();
        var objectType = Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);

        return TryCompleteObjectValue(parentSelection, objectType, data, errorTrie, depth, parent);
    }

    private bool TryCompleteObjectValue(
        Selection parentSelection,
        IObjectTypeDefinition objectType,
        SourceResultElement source,
        ErrorTrie? errorTrie,
        int depth,
        CompositeResultElement target)
    {
        AssertDepthAllowed(ref depth);

        // if the property value is yet undefined we need to initialize it
        // with the current selection set.
        if (target.ValueKind is JsonValueKind.Undefined)
        {
            var operation = parentSelection.DeclaringSelectionSet.DeclaringOperation;
            var selectionSet = operation.GetSelectionSet(parentSelection, objectType);
            target.SetValue(selectionSet);
        }

        foreach (var property in source.EnumerateObject())
        {
            if (!target.TryGetProperty(property.NameSpan, out var targetProperty))
            {
                continue;
            }

            var selection =  targetProperty.GetRequiredSelection();

            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            if (!TryCompleteValue(
                selection,
                selection.Type,
                property.Value,
                errorTrieForResponseName,
                depth,
                targetProperty))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryCompleteAbstractValue(
        Selection selection,
        IType type,
        SourceResultElement source,
        ErrorTrie? errorTrie,
        int depth,
        CompositeResultElement target)
        => TryCompleteObjectValue(
            selection,
            GetType(type, source),
            source,
            errorTrie,
            depth,
            target);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IObjectTypeDefinition GetType(IType type, SourceResultElement data)
    {
        var namedType = type.NamedType();

        if (namedType is IObjectTypeDefinition objectType)
        {
            return objectType;
        }

        var typeName = data.GetProperty(IntrospectionFieldNames.TypeNameSpan).GetRequiredString();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrUndefined(this CompositeResultElement element)
        => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;
}
