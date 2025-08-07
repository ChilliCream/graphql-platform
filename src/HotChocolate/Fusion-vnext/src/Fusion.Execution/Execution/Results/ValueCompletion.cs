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

    public ValueCompletion(
        ISchemaDefinition schema,
        ResultPoolSession resultPoolSession,
        ErrorHandling errorHandling,
        int maxDepth,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(resultPoolSession);

        _schema = schema;
        _resultPoolSession = resultPoolSession;
        _includeFlags = includeFlags;
        _maxDepth = maxDepth;
        _errorHandling = errorHandling;
    }

    public bool BuildResult(
        SelectionSet selectionSet,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        ObjectResult objectResult)
    {
        // we need to validate the data and create a GraphQL error if its not an object.
        foreach (var selection in selectionSet.Selections)
        {
            if (!selection.IsIncluded(_includeFlags))
            {
                continue;
            }

            var fieldResult = objectResult[selection.ResponseName];

            if (data.TryGetProperty(selection.ResponseName, out var element)
                && !TryCompleteValue(selection, selection.Type, sourceSchemaResult, element, 0, fieldResult))
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

        return true;
    }

    private bool TryCompleteValue(
        Selection selection,
        IType type,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        int depth,
        ResultData parent)
    {
        if (type.Kind is TypeKind.NonNull)
        {
            if (data.IsNullOrUndefined() && _errorHandling is ErrorHandling.Propagate)
            {
                parent.SetNextValueNull();
                return false;
            }

            type = type.InnerType();
        }

        if (data.IsNullOrUndefined())
        {
            parent.SetNextValueNull();
            return true;
        }

        if (type.Kind is TypeKind.List)
        {
            return TryCompleteList(selection, type, sourceSchemaResult, data, depth, parent);
        }

        if (type.Kind is TypeKind.Object)
        {
            return TryCompleteObjectValue(selection, type, sourceSchemaResult, data, depth, parent);
        }

        if (type.Kind is TypeKind.Interface or TypeKind.Union)
        {
            return TryCompleteAbstractValue(selection, type, sourceSchemaResult, data, depth, parent);
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
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
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

#if NET9_0_OR_GREATER
        for (int i = 0, len = data.GetArrayLength(); i < len; ++i)
        {
            var item = data[i];
#else
        foreach (var item in data.EnumerateArray())
        {
#endif
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

            if (!HandleElement(item))
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

        bool HandleElement(in JsonElement item)
        {
            if (isNested)
            {
                return TryCompleteList(selection, elementType, sourceSchemaResult, item, depth, listResult);
            }
            else if (isLeaf)
            {
                listResult.SetNextValue(item);
                return true;
            }
            else
            {
                return TryCompleteObjectValue(selection, elementType, sourceSchemaResult, item, depth, listResult);
            }
        }
    }

    private bool TryCompleteObjectValue(
        Selection selection,
        IType type,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        int depth,
        ResultData parent)
    {
        var namedType = type.NamedType();
        var objectType = Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);

        return TryCompleteObjectValue(selection, objectType, sourceSchemaResult, data, depth, parent);
    }

    private bool TryCompleteObjectValue(
        Selection selection,
        IObjectTypeDefinition objectType,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        int depth,
        ResultData parent)
    {
        AssertDepthAllowed(ref depth);

        var operation = selection.DeclaringSelectionSet.DeclaringOperation;
        var selectionSet = operation.GetSelectionSet(selection, objectType);
        var objectResult = _resultPoolSession.RentObjectResult();

        objectResult.Initialize(_resultPoolSession, selectionSet, _includeFlags);

        foreach (var field in objectResult.Fields)
        {
            var fieldSelection = field.Selection;

            if (!fieldSelection.IsIncluded(_includeFlags))
            {
                continue;
            }

            if (data.TryGetProperty(fieldSelection.ResponseName, out var child)
                && !TryCompleteValue(fieldSelection, fieldSelection.Type, sourceSchemaResult, child, depth, field))
            {
                parent.SetNextValueNull();
                return false;
            }
        }

        parent.SetNextValue(objectResult);
        return true;
    }

    private bool TryCompleteAbstractValue(
        Selection selection,
        IType type,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        int depth,
        ResultData parent)
        => TryCompleteObjectValue(
            selection,
            GetType(type, data),
            sourceSchemaResult,
            data,
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
