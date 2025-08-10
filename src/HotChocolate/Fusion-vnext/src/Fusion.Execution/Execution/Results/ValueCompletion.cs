using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
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

    public bool BuildResult(
        SelectionSet selectionSet,
        JsonElement data,
        ErrorTrie? errorTrie,
        ObjectResult objectResult)
    {
        if (data is not { ValueKind: JsonValueKind.Object })
        {
            // If we encounter a null, we check if there's an error on this field
            // or somewhere below. If there is, we add the error, since it likely
            // propagated and erased the current field result.
            if (errorTrie?.FindPathToFirstError() is { } pathToFirstError)
            {
                var path = Path.FromList(pathToFirstError.Path);

                var errorWithPath = ErrorBuilder.FromError(pathToFirstError.Error)
                    .SetPath(objectResult.Path.Append(path))

                    // We should add the location here, but not sure if it's worth it to iterate the
                    // selections for it.
                    .Build();

                _errors.Add(errorWithPath);
            }

            return false;
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
                    var parentIndex = fieldResult.ParentIndex;
                    var parent = fieldResult.Parent;

                    if (_errorHandling is ErrorHandling.Propagate)
                    {
                        while (parent is not null)
                        {
                            if (parent.TrySetValueNull(parentIndex))
                            {
                                break;
                            }
                            else
                            {
                                parentIndex = parent.ParentIndex;
                                parent = parent.Parent;

                                if (parent is null)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    else
                    {
                        fieldResult?.TrySetValueNull(parentIndex);
                    }
                }
            }
        }

        return true;
    }

    private bool TryCompleteValue(
        Selection selection,
        IType type,
        JsonElement data,
        ErrorTrie? errorTrie,
        int depth,
        ResultData parent)
    {
        if (type.Kind is TypeKind.NonNull)
        {
            if (data.IsNullOrUndefined() && _errorHandling is ErrorHandling.Propagate)
            {
                parent.SetNextValueNull();
                if (errorTrie?.Error is { } error)
                {
                    var errorWithPath = ErrorBuilder.FromError(error)
                        .SetPath(parent.Path)
                        .AddLocation(selection.SyntaxNodes[0].Node)
                        .Build();
                    _errors.Add(errorWithPath);
                }
                return false;
            }

            type = type.InnerType();
        }

        if (data.IsNullOrUndefined())
        {
            parent.SetNextValueNull();
            if (errorTrie?.Error is { } error)
            {
                var errorWithPath = ErrorBuilder.FromError(error)
                    .SetPath(parent.Path)
                    .AddLocation(selection.SyntaxNodes[0].Node)
                    .Build();
                _errors.Add(errorWithPath);
            }
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

            if (item.IsNullOrUndefined())
            {
                if (!isNullable && _errorHandling is ErrorHandling.Propagate)
                {
                    parent.SetNextValueNull();
                    return false;
                }

                listResult.SetNextValueNull();

                continue;
            }

            if (!HandleElement(item, i))
            {
                if (!isNullable)
                {
                    parent.SetNextValueNull();
                    return false;
                }

                listResult.SetNextValueNull();
            }
        }

        parent.SetNextValue(listResult);
        return true;

        bool HandleElement(in JsonElement item, int index)
        {
            ErrorTrie? errorTrieForIndex = null;
            errorTrie?.TryGetValue(index, out errorTrieForIndex);

            if (isNested)
            {
                return TryCompleteList(selection, elementType, item, errorTrieForIndex, depth, listResult);
            }
            else if (isLeaf)
            {
                listResult.SetNextValue(item);
                return true;
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
        objectResult.SetParent(parent, parent.ParentIndex);

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
                    parent.SetNextValueNull();
                    return false;
                }
            }
        }

        parent.SetNextValue(objectResult);
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
