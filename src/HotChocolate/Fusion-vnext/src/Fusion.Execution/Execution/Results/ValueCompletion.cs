using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal sealed class ValueCompletion
{
    private readonly ISchemaDefinition _schema;
    private readonly ResultPoolSession _resultPoolSession;
    private readonly ErrorHandling _errorHandling;
    private readonly int _maxDepth;
    private readonly ulong _includeFlags;
    private readonly List<IError> _errors;

    public ValueCompletion(
        ISchemaDefinition schema,
        ResultPoolSession resultPoolSession,
        ErrorHandling errorHandling,
        int maxDepth,
        ulong includeFlags,
        List<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(resultPoolSession);
        ArgumentNullException.ThrowIfNull(errors);

        _schema = schema;
        _resultPoolSession = resultPoolSession;
        _errorHandling = errorHandling;
        _maxDepth = maxDepth;
        _includeFlags = includeFlags;
        _errors = errors;
    }

    public void BuildResult(
        SelectionSet selectionSet,
        JsonElement data,
        ErrorTrie? errorTrie,
        ReadOnlySpan<string> responseNames,
        ObjectResult objectResult)
    {
        if (data is not { ValueKind: JsonValueKind.Object })
        {
            var error = errorTrie?.FindFirstError() ??
                ErrorBuilder.New()
                    .SetMessage("Unexpected Execution Error")
                    .Build();

            BuildErrorResult(objectResult, responseNames, error, objectResult.Path);

            return;
        }

        foreach (var selection in selectionSet.Selections)
        {
            if (!selection.IsIncluded(_includeFlags))
            {
                continue;
            }

            var fieldResult = objectResult[selection.ResponseName];

            if (data.TryGetProperty(selection.ResponseName, out var element))
            {
                ErrorTrie? errorTrieForResponseName = null;
                errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

                if (!TryCompleteValue(selection, selection.Type, element, errorTrieForResponseName, 0, fieldResult))
                {
                    if (_errorHandling is ErrorHandling.Propagate)
                    {
                        PropagateNullValues(objectResult);
                    }
                }
            }
        }
    }

    public void BuildErrorResult(
        ObjectResult objectResult,
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

            _errors.Add(errorWithPath);

            if (_errorHandling is ErrorHandling.Propagate && fieldResult.Selection.Type.IsNonNullType())
            {
                PropagateNullValues(objectResult);

                return;
            }
        }
    }

    private static void PropagateNullValues(ResultData result)
    {
        if (result.IsInvalidated)
        {
            return;
        }

        result.IsInvalidated = true;

        while (result.Parent is not null)
        {
            var index = result.ParentIndex;
            var parent = result.Parent;

            if (parent.IsInvalidated)
            {
                return;
            }

            if (parent.TrySetValueNull(index))
            {
                return;
            }

            parent.IsInvalidated = true;

            result = parent;
        }
    }

    private bool TryCompleteValue(
        Selection selection,
        IType type,
        JsonElement data,
        ErrorTrie? errorTrie,
        int depth,
        ResultData parent)
    {
        if (errorTrie?.Error is { } error)
        {
            var errorWithPath = ErrorBuilder.FromError(error)
                .SetPath(parent.Path)
                .AddLocation(selection.SyntaxNodes[0].Node)
                .Build();
            _errors.Add(errorWithPath);
        }

        if (type.Kind is TypeKind.NonNull)
        {
            if (data.IsNullOrUndefined() && _errorHandling is ErrorHandling.Propagate)
            {
                return false;
            }

            type = type.InnerType();
        }

        if (data.IsNullOrUndefined())
        {
            return true;
        }

        if (type.Kind is TypeKind.List)
        {
            return TryCompleteList(selection, type, data, errorTrie, depth, parent);
        }

        if (type.Kind is TypeKind.Object)
        {
            return TryCompleteObjectValue(selection, type, data, errorTrie, depth, parent);
        }

        if (type.Kind is TypeKind.Interface or TypeKind.Union)
        {
            return TryCompleteAbstractValue(selection, type, data, errorTrie, depth, parent);
        }

        if (type.Kind is TypeKind.Scalar or TypeKind.Enum)
        {
            parent.SetNextValue(data);
            return true;
        }

        throw new NotSupportedException($"The type {type} is not supported.");
    }

    private bool TryCompleteList(
        Selection selection,
        IType type,
        JsonElement data,
        ErrorTrie? errorTrie,
        int depth,
        ResultData parent)
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

        for (int i = 0, len = data.GetArrayLength(); i < len; ++i)
        {
            var item = data[i];
            ErrorTrie? errorTrieForIndex = null;
            errorTrie?.TryGetValue(i, out errorTrieForIndex);

            if (errorTrieForIndex?.Error is { } error)
            {
                var errorWithPath = ErrorBuilder.FromError(error)
                    .SetPath(parent.Path.Append(i))
                    .AddLocation(selection.SyntaxNodes[0].Node)
                    .Build();
                _errors.Add(errorWithPath);
            }

            if (item.IsNullOrUndefined())
            {
                if (!isNullable && _errorHandling is ErrorHandling.Propagate)
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
        Selection selection,
        IType type,
        JsonElement data,
        ErrorTrie? errorTrie,
        int depth,
        ResultData parent)
    {
        var namedType = type.NamedType();
        var objectType = Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);

        return TryCompleteObjectValue(selection, objectType, data, errorTrie, depth, parent);
    }

    private bool TryCompleteObjectValue(
        Selection selection,
        IObjectTypeDefinition objectType,
        JsonElement data,
        ErrorTrie? errorTrie,
        int depth,
        ResultData parent)
    {
        AssertDepthAllowed(ref depth);

        var operation = selection.DeclaringSelectionSet.DeclaringOperation;
        var selectionSet = operation.GetSelectionSet(selection, objectType);
        var objectResult = _resultPoolSession.RentObjectResult();

        objectResult.Initialize(_resultPoolSession, selectionSet, _includeFlags);

        // we set the value early so that in the error case we can correctly
        // traverse along the parent path and propagate errors.
        parent.SetNextValue(objectResult);

        foreach (var field in objectResult.Fields)
        {
            var fieldSelection = field.Selection;

            if (!fieldSelection.IsIncluded(_includeFlags))
            {
                continue;
            }

            if (data.TryGetProperty(fieldSelection.ResponseName, out var child))
            {
                ErrorTrie? errorTrieForResponseName = null;
                errorTrie?.TryGetValue(fieldSelection.ResponseName, out errorTrieForResponseName);

                if (!TryCompleteValue(fieldSelection, fieldSelection.Type, child, errorTrieForResponseName, depth, field))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool TryCompleteAbstractValue(
        Selection selection,
        IType type,
        JsonElement data,
        ErrorTrie? errorTrie,
        int depth,
        ResultData parent)
        => TryCompleteObjectValue(
            selection,
            GetType(type, data),
            data,
            errorTrie,
            depth,
            parent);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IObjectTypeDefinition GetType(IType type, JsonElement data)
    {
        var namedType = type.NamedType();

        if (namedType is IObjectTypeDefinition objectType)
        {
            return objectType;
        }

        var typeName = data.GetProperty(IntrospectionFieldNames.TypeNameSpan).GetString()!;
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
    public static bool IsNullOrUndefined(this JsonElement element)
        => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;
}
