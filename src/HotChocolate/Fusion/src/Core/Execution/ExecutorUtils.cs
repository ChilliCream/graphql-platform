using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Execution.Processing;
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
        WorkItem workItem)
        => ComposeResult(
            context,
            (SelectionSet)workItem.SelectionSet,
            workItem.SelectionSetData,
            workItem.SelectionSetResult);

    private static void ComposeResult(
        FusionExecutionContext context,
        SelectionSet selectionSet,
        SelectionData[] selectionSetData,
        ObjectResult selectionSetResult)
    {
        var count = selectionSet.Selections.Count;
        ref var selection = ref selectionSet.GetSelectionsReference();
        ref var result = ref selectionSetResult.GetReference();
        ref var data = ref MemoryMarshal.GetArrayDataReference(selectionSetData);
        ref var endSelection = ref Unsafe.Add(ref selection, count);

        while(Unsafe.IsAddressLessThan(ref selection, ref endSelection))
        {
            var selectionType = selection.Type;
            var responseName = selection.ResponseName;
            var field = selection.Field;

            if (!field.IsIntrospectionField)
            {
                var nullable = selection.TypeKind is not TypeKind.NonNull;
                var namedType = selectionType.NamedType();

                if (namedType.IsType(TypeKind.Scalar))
                {
                    var value = data.Single.Element;
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
                    result.Set(responseName, value, nullable);
                }
                else if (selectionType.IsCompositeType())
                {
                    var value = ComposeObject(context, selection, data);
                    result.Set(responseName, value, nullable);
                }
                else
                {
                    var value = ComposeList(context, selection, data, selectionType);
                    result.Set(responseName, value, nullable);
                }
            }
            else if ((selection.CustomOptions & _typeNameFlag) == _typeNameFlag)
            {
                var value = selection.DeclaringType.Name;
                result.Set(responseName, value, false);
            }

            // move our pointers
            selection = ref Unsafe.Add(ref selection, 1);
            result = ref Unsafe.Add(ref result, 1);
            data = ref Unsafe.Add(ref data, 1);
        }
    }

    private static ListResult? ComposeList(
        FusionExecutionContext context,
        ISelection selection,
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

        var elementType = type.ElementType();
        var result = context.Result.RentList(json.GetArrayLength());

        if (!elementType.IsType(TypeKind.List))
        {
            foreach (var item in json.EnumerateArray())
            {
                result.AddUnsafe(
                    ComposeObject(
                        context,
                        selection,
                        new SelectionData(new JsonResult(schemaName, item))));
            }
        }
        else
        {
            foreach (var item in json.EnumerateArray())
            {
                result.AddUnsafe(
                    ComposeList(
                        context,
                        selection,
                        new SelectionData(new JsonResult(schemaName, item)),
                        elementType));
            }
        }

        return result;
    }

    private static ObjectResult? ComposeObject(
        FusionExecutionContext context,
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
            var typeMetadata = context.Configuration.GetType<ObjectTypeInfo>(typeInfo);
            type = context.Schema.GetType<ObjectType>(typeMetadata.Name);
        }

        var selectionSet = (SelectionSet)context.Operation.GetSelectionSet(selection, type);
        var selectionCount = selectionSet.Selections.Count;
        var result = context.Result.RentObject(selectionCount);

        if (context.NeedsMoreData(selectionSet))
        {
            context.RegisterState(selectionSet, result, selectionData);
        }
        else
        {
            var childSelectionResults = new SelectionData[selectionCount];
            ExtractSelectionResults(selectionData, selectionSet, childSelectionResults);
            ComposeResult(context, selectionSet, childSelectionResults, result);
        }

        return result;
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

            while(Unsafe.IsAddressLessThan(ref selection, ref endSelection))
            {
                if (data.TryGetProperty(selection.ResponseName, out var value))
                {
                    selectionData = selectionData.AddResult(new JsonResult(schemaName, value));
                }

                selection = ref Unsafe.Add(ref selection, 1);
                selectionData = ref Unsafe.Add(ref selectionData, 1);
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

                while(Unsafe.IsAddressLessThan(ref selection, ref endSelection))
                {
                    if (element.TryGetProperty(selection.ResponseName, out var value))
                    {
                        selectionData = selectionData.AddResult(new JsonResult(schemaName, value));
                    }

                    selection = ref Unsafe.Add(ref selection, 1);
                    selectionData = ref Unsafe.Add(ref selectionData, 1);
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

        while(Unsafe.IsAddressLessThan(ref currentSelection, ref endSelection))
        {
            if (data.TryGetProperty(currentSelection.ResponseName, out var property))
            {
                currentResult = currentResult.HasValue
                    ? currentResult.AddResult(new(schemaName, property))
                    : new(new JsonResult(schemaName, property));
            }

            currentSelection = ref Unsafe.Add(ref currentSelection, 1);
            currentResult = ref Unsafe.Add(ref currentResult, 1);
        }
    }

    public static void ExtractPartialResult(WorkItem workItem)
    {
        // capture the partial result available
        var partialResult = workItem.SelectionSetData[0];

        // if we have a partial result available lets unwrap it.
        if (partialResult.HasValue)
        {
            // first we need to erase the partial result from the array so that its not
            // combined into the result creation.
            workItem.SelectionSetData[0] = default;

            // next we will unwrap the results.
            ExtractSelectionResults(
                partialResult,
                (SelectionSet)workItem.SelectionSet,
                workItem.SelectionSetData);

            // last we will check if there are any exports for this selection-set.
            ExtractVariables(
                partialResult,
                workItem.ExportKeys,
                workItem.VariableValues);
        }
    }

    public static void ExtractErrors(
        ResultBuilder resultBuilder,
        JsonElement errors,
        bool addDebugInfo)
    {
        if (errors.ValueKind is not JsonValueKind.Array)
        {
            return;
        }

        foreach (var error in errors.EnumerateArray())
        {
            ExtractError(resultBuilder, error, addDebugInfo);
        }
    }

    private static void ExtractError(
        ResultBuilder resultBuilder,
        JsonElement error,
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
                // TODO : rewrite remote path if possible!

                if (addDebugInfo)
                {
                    errorBuilder.SetExtension("remotePath", remotePath);
                }
            }

            if (error.TryGetProperty("locations", out var locations) &&
                locations.ValueKind is JsonValueKind.Array)
            {
                foreach (var location in extensions.EnumerateArray())
                {
                    if (location.TryGetProperty("line", out var lineValue) &&
                        location.TryGetProperty("column", out var columnValue) &&
                        lineValue.TryGetInt32(out var line)&&
                        columnValue.TryGetInt32(out var column))
                    {
                        errorBuilder.AddLocation(line, column);
                    }
                }
            }

            resultBuilder.AddError(errorBuilder.Build());
        }
    }

    public static void ExtractVariables(
        SelectionData parent,
        IReadOnlyList<string> exportKeys,
        Dictionary<string, IValueNode> variableValues)
    {
        if (exportKeys.Count > 0)
        {
            if (parent.Multiple is null)
            {
                ExtractVariables(parent.Single.Element, exportKeys, variableValues);
            }
            else
            {
                foreach (var result in parent.Multiple)
                {
                    ExtractVariables(result.Element, exportKeys, variableValues);
                }
            }
        }
    }

    public static void ExtractVariables(
        JsonElement parent,
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
                    variableValues.TryAdd(key, JsonValueToGraphQLValueConverter.Convert(property));
                }
            }
        }
    }
}
