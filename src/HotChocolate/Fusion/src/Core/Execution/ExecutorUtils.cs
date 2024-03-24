using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.Selection;
using IType = HotChocolate.Types.IType;
using ObjectType = HotChocolate.Types.ObjectType;

namespace HotChocolate.Fusion.Execution;

internal static class ExecutorUtils
{
    private const CustomOptionsFlags _reEncodeIdFlag =
        (CustomOptionsFlags)ObjectFieldFlags.ReEncodeId;

    private const CustomOptionsFlags _typeNameFlag =
        (CustomOptionsFlags)ObjectFieldFlags.TypeName;

    public static void ComposeResult(
        FusionExecutionContext context,
        ExecutionState executionState)
        => ComposeResult(
            context,
            executionState.SelectionSet,
            executionState.SelectionSetData,
            executionState.SelectionSetResult);

    private static void ComposeResult(
        FusionExecutionContext context,
        SelectionSet selectionSet,
        SelectionData[] selectionSetData,
        ObjectResult selectionSetResult,
        bool partialResult = false)
    {
        if (selectionSetResult.IsInvalidated)
        {
            return;
        }

        var count = selectionSet.Selections.Count;
        ref var selection = ref selectionSet.GetSelectionsReference();
        ref var result = ref selectionSetResult.GetReference();
        ref var data = ref MemoryMarshal.GetArrayDataReference(selectionSetData);
        ref var endSelection = ref Unsafe.Add(ref selection, count);
        var responseIndex = 0;

        while (Unsafe.IsAddressLessThan(ref selection, ref endSelection))
        {
            var selectionType = selection.Type;
            var responseName = selection.ResponseName;
            var field = selection.Field;

            if (!field.IsIntrospectionField)
            {
                var nullable = selection.TypeKind is not TypeKind.NonNull;
                var namedType = selectionType.NamedType();

                if (!data.HasValue)
                {
                    if (!partialResult)
                    {
                        if (!nullable)
                        {
                            PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                            break;
                        }

                        result.Set(responseName, null, nullable);
                    }
                }
                else if (namedType.IsType(TypeKind.Scalar))
                {
                    var value = data.Single.Element;

                    if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined && !nullable)
                    {
                        PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                        break;
                    }

                    result.Set(responseName, value, nullable);

                    if (value.ValueKind is JsonValueKind.String &&
                        (selection.CustomOptions & _reEncodeIdFlag) == _reEncodeIdFlag)
                    {
                        var subgraphName = data.Single.SubgraphName;
                        var reformattedId = context.ReformatId(value.GetString()!, subgraphName);
                        result.Set(responseName, reformattedId, nullable);
                    }
                }
                else if (namedType.IsType(TypeKind.Enum))
                {
                    // we might need to map the enum value!
                    var value = data.Single.Element;

                    if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined &&
                        !nullable)
                    {
                        PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                        break;
                    }

                    result.Set(responseName, value, nullable);
                }
                else if (selectionType.IsCompositeType())
                {
                    if (!result.IsInitialized)
                    {
                        var value = ComposeObject(
                            context,
                            selectionSetResult,
                            responseIndex,
                            selection,
                            data);

                        if (value is null && !nullable)
                        {
                            PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                            break;
                        }

                        result.Set(responseName, value, nullable);
                    }
                }
                else
                {
                    if (!result.IsInitialized)
                    {
                        var value = ComposeList(
                            context,
                            selectionSetResult,
                            responseIndex,
                            selection,
                            data,
                            selectionType);

                        if (value is null && !nullable)
                        {
                            PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                            break;
                        }

                        result.Set(responseName, value, nullable);
                    }
                }
            }
            else if ((selection.CustomOptions & _typeNameFlag) == _typeNameFlag)
            {
                var value = selection.DeclaringType.Name;
                result.Set(responseName, value, false);
            }

            if (selectionSetResult.IsInvalidated)
            {
                return;
            }

            // REMEMBER: this counter needs to be only raised if the selection is included.
            // ALSO: data ref needs to change once we do this.
            responseIndex++;

            // move our pointers
            selection = ref Unsafe.Add(ref selection, 1)!;
            result = ref Unsafe.Add(ref result, 1)!;
            data = ref Unsafe.Add(ref data, 1);
        }
    }

    private static ListResult? ComposeList(
        FusionExecutionContext context,
        ResultData parent,
        int parentIndex,
        Selection selection,
        SelectionData selectionData,
        IType type)
    {
        if (selectionData.IsNull())
        {
            return null;
        }

        var json = selectionData.Single.Element;
        var schemaName = selectionData.Single.SubgraphName;
        Debug.Assert(selectionData.Multiple is null, "selectionResult.Multiple is null");
        Debug.Assert(json.ValueKind is JsonValueKind.Array, "json.ValueKind is JsonValueKind.Array");

        var index = 0;
        var elementType = type.ElementType();
        var nullable = elementType.IsNullableType();
        var result = context.Result.RentList(json.GetArrayLength());

        result.IsNullable = nullable;
        result.SetParent(parent, parentIndex);

        foreach (var item in json.EnumerateArray())
        {
            // we add a placeholder here so if the ComposeElement propagates an error
            // there is a value here.
            result.AddUnsafe(null);

            var element = ComposeElement(
                context,
                result,
                index,
                selection,
                new SelectionData(new JsonResult(schemaName, item)),
                elementType);

            if (!nullable && element is null)
            {
                PropagateNullValues(context.Result, selection, result, index);
                return null;
            }

            result.SetUnsafe(index++, element);

            if (result.IsInvalidated)
            {
                return null;
            }
        }

        return result;
    }

    private static object? ComposeElement(
        FusionExecutionContext context,
        ResultData parent,
        int parentIndex,
        Selection selection,
        SelectionData selectionData,
        IType valueType)
    {
        var namedType = valueType.NamedType();

        if (!selectionData.HasValue)
        {
            return null;
        }

        if (namedType.IsType(TypeKind.Scalar))
        {
            var value = selectionData.Single.Element;

            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (value.ValueKind is JsonValueKind.String &&
                (selection.CustomOptions & _reEncodeIdFlag) == _reEncodeIdFlag)
            {
                var subgraphName = selectionData.Single.SubgraphName;
                return context.ReformatId(value.GetString()!, subgraphName);
            }

            return value;
        }

        if (namedType.IsType(TypeKind.Enum))
        {
            var value = selectionData.Single.Element;

            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            return value;
        }

        return TypeExtensions.IsCompositeType(valueType)
            ? ComposeObject(context, parent, parentIndex, selection, selectionData)
            : ComposeList(context, parent, parentIndex, selection, selectionData, valueType);
    }

    private static ObjectResult? ComposeObject(
        FusionExecutionContext context,
        ResultData parent,
        int parentIndex,
        ISelection selection,
        SelectionData selectionData)
    {
        if (selectionData.IsNull())
        {
            return null;
        }

        ObjectType type;

        if (selection.Type.NamedType() is ObjectType ot)
        {
            type = ot;
        }
        else
        {
            var typeInfo = selectionData.GetTypeName();
            var typeMetadata = context.Configuration.GetType<ObjectTypeMetadata>(typeInfo);
            type = context.Schema.GetType<ObjectType>(typeMetadata.Name);
        }

        var selectionSet = Unsafe.As<SelectionSet>(context.Operation.GetSelectionSet(selection, type));
        var selectionCount = selectionSet.Selections.Count;
        var result = context.Result.RentObject(selectionCount);

        result.SetParent(parent, parentIndex);

        if (context.NeedsMoreData(selectionSet))
        {
            context.TryRegisterState(selectionSet, result, selectionData);

            var childSelectionResults = new SelectionData[selectionCount];
            ExtractSelectionResults(selectionData, selectionSet, childSelectionResults);
            ComposeResult(context, selectionSet, childSelectionResults, result, true);
        }
        else
        {
            var childSelectionResults = new SelectionData[selectionCount];
            ExtractSelectionResults(selectionData, selectionSet, childSelectionResults);
            ComposeResult(context, selectionSet, childSelectionResults, result);
        }

        return result.IsInvalidated ? null : result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsType(this IType type, TypeKind kind)
    {
        if (type.Kind == kind)
        {
            return true;
        }

        if (type.Kind == TypeKind.NonNull && ((NonNullType)type).Type.Kind == kind)
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCompositeType(this IType type)
    {
        if (type.Kind is TypeKind.Object or TypeKind.Interface or TypeKind.Union)
        {
            return true;
        }

        if (type.Kind == TypeKind.NonNull)
        {
            var innerKind = ((NonNullType)type).Type.Kind;
            return innerKind is TypeKind.Object or TypeKind.Interface or TypeKind.Union;
        }

        return false;
    }

    private static void ExtractSelectionResults(
        SelectionData parent,
        SelectionSet selectionSet,
        SelectionData[] selectionSetData)
    {
        var selectionCount = selectionSet.Selections.Count;

        if (parent.Multiple is null)
        {
            var schemaName = parent.Single.SubgraphName;
            var data = parent.Single.Element;

            ref var selection = ref selectionSet.GetSelectionsReference();
            ref var selectionData = ref MemoryMarshal.GetArrayDataReference(selectionSetData);
            ref var endSelection = ref Unsafe.Add(ref selection, selectionCount);

            while (Unsafe.IsAddressLessThan(ref selection, ref endSelection))
            {
                if (data.TryGetProperty(selection.ResponseName, out var value))
                {
                    selectionData = selectionData.AddResult(new JsonResult(schemaName, value));
                }

                selection = ref Unsafe.Add(ref selection, 1)!;
                selectionData = ref Unsafe.Add(ref selectionData, 1)!;
            }
        }
        else
        {
            foreach (var result in parent.Multiple)
            {
                var schemaName = result.SubgraphName;
                var element = result.Element;

                ref var selection = ref selectionSet.GetSelectionsReference();
                ref var selectionData = ref MemoryMarshal.GetArrayDataReference(selectionSetData);
                ref var endSelection = ref Unsafe.Add(ref selection, selectionCount);

                while (Unsafe.IsAddressLessThan(ref selection, ref endSelection))
                {
                    if (element.TryGetProperty(selection.ResponseName, out var value))
                    {
                        selectionData = selectionData.AddResult(new JsonResult(schemaName, value));
                    }

                    selection = ref Unsafe.Add(ref selection, 1)!;
                    selectionData = ref Unsafe.Add(ref selectionData, 1)!;
                }
            }
        }
    }

    public static void ExtractSelectionResults(
        SelectionSet selectionSet,
        string schemaName,
        JsonElement data,
        SelectionData[] selectionResults)
    {
        if (data.ValueKind is not JsonValueKind.Object)
        {
            return;
        }

        ref var currentSelection = ref selectionSet.GetSelectionsReference();
        ref var currentResult = ref MemoryMarshal.GetArrayDataReference(selectionResults);
        ref var endSelection = ref Unsafe.Add(ref currentSelection, selectionSet.Selections.Count);

        while (Unsafe.IsAddressLessThan(ref currentSelection, ref endSelection))
        {
            if (data.TryGetProperty(currentSelection.ResponseName, out var property))
            {
                currentResult = currentResult.HasValue
                    ? currentResult.AddResult(new(schemaName, property))
                    : new(new JsonResult(schemaName, property));
            }

            currentSelection = ref Unsafe.Add(ref currentSelection, 1)!;
            currentResult = ref Unsafe.Add(ref currentResult, 1)!;
        }
    }

    public static void TryInitializeExecutionState(QueryPlan queryPlan, ExecutionState executionState)
    {
        if (executionState.IsInitialized)
        {
            return;
        }

        // capture the partial result available
        var partialResult = executionState.SelectionSetData[0];

        // if we have a partial result available lets unwrap it.
        if (partialResult.HasValue)
        {
            // first we need to erase the partial result from the array so that its not
            // combined into the result creation.
            executionState.SelectionSetData[0] = default;

            // next we will unwrap the results.
            ExtractSelectionResults(
                partialResult,
                executionState.SelectionSet,
                executionState.SelectionSetData);

            // last we will check if there are any exports for this selection-set.
            ExtractVariables(
                partialResult,
                queryPlan,
                executionState.SelectionSet,
                executionState.Requires,
                executionState.VariableValues);
        }

        executionState.IsInitialized = true;
    }

    public static void ExtractErrors(
        ResultBuilder resultBuilder,
        JsonElement errors,
        ObjectResult selectionSetResult,
        int pathDepth,
        bool addDebugInfo)
    {
        if (errors.ValueKind is not JsonValueKind.Array)
        {
            return;
        }

        var path = PathHelper.CreatePathFromContext(selectionSetResult);
        foreach (var error in errors.EnumerateArray())
        {
            ExtractError(resultBuilder, error, path, pathDepth, addDebugInfo);
        }
    }

    private static void ExtractError(
        ResultBuilder resultBuilder,
        JsonElement error,
        Path parentPath,
        int pathDepth,
        bool addDebugInfo)
    {
        if (error.ValueKind is not JsonValueKind.Object)
        {
            return;
        }

        if (error.TryGetProperty("message", out var message) &&
            message.ValueKind is JsonValueKind.String)
        {
            var errorBuilder = new ErrorBuilder();
            errorBuilder.SetMessage(message.GetString()!);

            if (error.TryGetProperty("code", out var code) &&
                code.ValueKind is JsonValueKind.String)
            {
                errorBuilder.SetCode(code.GetString()!);
            }

            if (error.TryGetProperty("extensions", out var extensions) &&
                extensions.ValueKind is JsonValueKind.Object)
            {
                foreach (var property in extensions.EnumerateObject())
                {
                    errorBuilder.SetExtension(property.Name, property.Value);
                }
            }

            if (error.TryGetProperty("path", out var remotePath) &&
                remotePath.ValueKind is JsonValueKind.Array)
            {
                var path = PathHelper.CombinePath(parentPath, remotePath, pathDepth);
                errorBuilder.SetPath(path);

                if (addDebugInfo)
                {
                    errorBuilder.SetExtension("remotePath", remotePath);
                }
            }

            if (addDebugInfo &&
                error.TryGetProperty("locations", out var locations) &&
                locations.ValueKind is JsonValueKind.Array)
            {
                var remoteLocations = new List<JsonElement>();
                foreach (var location in locations.EnumerateArray())
                {
                    if (location.TryGetProperty("line", out var lineValue) &&
                        location.TryGetProperty("column", out var columnValue) &&
                        lineValue.TryGetInt32(out _) &&
                        columnValue.TryGetInt32(out _))
                    {
                        remoteLocations.Add(location);
                    }
                }

                if (remoteLocations.Count > 0)
                {
                    errorBuilder.SetExtension("remoteLocations", remoteLocations);
                }
            }

            resultBuilder.AddError(errorBuilder.Build());
        }
    }

    public static void ExtractVariables(
        SelectionData parent,
        QueryPlan queryPlan,
        ISelectionSet selectionSet,
        IReadOnlyList<string> exportKeys,
        Dictionary<string, IValueNode> variableValues)
    {
        if (exportKeys.Count > 0)
        {
            if (parent.Multiple is null)
            {
                ExtractVariables(
                    parent.Single.Element,
                    queryPlan,
                    selectionSet,
                    exportKeys,
                    variableValues);
            }
            else
            {
                foreach (var result in parent.Multiple)
                {
                    ExtractVariables(
                        result.Element,
                        queryPlan,
                        selectionSet,
                        exportKeys,
                        variableValues);
                }
            }
        }
    }

    public static void ExtractVariables(
        JsonElement parent,
        QueryPlan queryPlan,
        ISelectionSet selectionSet,
        IReadOnlyList<string> exportKeys,
        Dictionary<string, IValueNode> variableValues)
    {
        if (parent.ValueKind is not JsonValueKind.Object)
        {
            return;
        }

        if (exportKeys.Count > 0 &&
            parent.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            for (var i = 0; i < exportKeys.Count; i++)
            {
                var key = exportKeys[i];

                if (!variableValues.ContainsKey(key) &&
                    parent.TryGetProperty(key, out var property))
                {
                    var path = queryPlan.GetExportPath(selectionSet, key);

                    if (path.Count >= 2)
                    {
                        for (var j = 1; j < path.Count; j++)
                        {
                            if (property.TryGetProperty(path[j], out var next))
                            {
                                property = next;
                            }
                            else
                            {
                                property = default;
                                break;
                            }
                        }
                    }

                    variableValues.TryAdd(key, JsonValueToGraphQLValueConverter.Convert(property));
                }
            }
        }
    }

    private static void PropagateNullValues(
        ResultBuilder resultBuilder,
        Selection selection,
        ResultData selectionSetResult,
        int responseIndex)
    {
        var path = PathHelper.CreatePathFromContext(selection, selectionSetResult, responseIndex);
        resultBuilder.AddNonNullViolation(selection, path);
        ValueCompletion.PropagateNullValues(selectionSetResult);
    }
}
