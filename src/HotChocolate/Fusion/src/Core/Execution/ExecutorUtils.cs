using System.Diagnostics;
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
            workItem.SelectionSet.Selections,
            workItem.SelectionResults,
            workItem.Result);

    private static void ComposeResult(
        FusionExecutionContext context,
        IReadOnlyList<ISelection> selections,
        IReadOnlyList<SelectionResult> selectionResults,
        ObjectResult selectionSetResult)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            var selection = (Selection)selections[i];
            var selectionType = selection.Type;
            var responseName = selection.ResponseName;
            var field = selection.Field;

            if (!field.IsIntrospectionField)
            {
                var selectionResult = selectionResults[i];
                var nullable = selection.TypeKind is not TypeKind.NonNull;
                var namedType = selectionType.NamedType();

                if (namedType.IsScalarType())
                {
                    var value = selectionResult.Single.Element;
                    selectionSetResult.SetValueUnsafe(i, responseName, value, nullable);

                    if (value.ValueKind is JsonValueKind.String &&
                        (selection.CustomOptions & _reEncodeIdFlag) == _reEncodeIdFlag)
                    {
                        var subgraphName = selectionResult.Single.SubgraphName;
                        var reformattedId = context.ReformatId(value.GetString()!, subgraphName);
                        selectionSetResult.SetValueUnsafe(i, responseName, reformattedId, nullable);
                    }
                }
                else if (namedType.IsEnumType())
                {
                    // we might need to map the enum value!
                    var value = selectionResult.Single.Element;
                    selectionSetResult.SetValueUnsafe(i, responseName, value, nullable);
                }
                else if (selectionType.IsCompositeType())
                {
                    var value = ComposeObject(context, selection, selectionResult);
                    selectionSetResult.SetValueUnsafe(i, responseName, value);
                }
                else
                {
                    var value = ComposeList(context, selection, selectionResult, selectionType);
                    selectionSetResult.SetValueUnsafe(i, responseName, value);
                }
            }
            else if ((selection.CustomOptions & _typeNameFlag) == _typeNameFlag)
            {
                var value = selection.DeclaringType.Name;
                selectionSetResult.SetValueUnsafe(i, responseName, value, false);
            }
        }
    }

    private static ListResult? ComposeList(
        FusionExecutionContext context,
        ISelection selection,
        SelectionResult selectionResult,
        IType type)
    {
        if (selectionResult.IsNull())
        {
            return null;
        }

        var json = selectionResult.Single.Element;
        var schemaName = selectionResult.Single.SubgraphName;
        Debug.Assert(selectionResult.Multiple is null, "selectionResult.Multiple is null");
        Debug.Assert(
            json.ValueKind is JsonValueKind.Array,
            "json.ValueKind is JsonValueKind.Array");

        var elementType = type.ElementType();
        var result = context.Result.RentList(json.GetArrayLength());

        if (!elementType.IsListType())
        {
            foreach (var item in json.EnumerateArray())
            {
                result.AddUnsafe(
                    ComposeObject(
                        context,
                        selection,
                        new SelectionResult(new JsonResult(schemaName, item))));
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
                        new SelectionResult(new JsonResult(schemaName, item)),
                        elementType));
            }
        }

        return result;
    }

    private static ObjectResult? ComposeObject(
        FusionExecutionContext context,
        ISelection selection,
        SelectionResult selectionResult)
    {
        if (selectionResult.IsNull())
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
            var typeInfo = selectionResult.GetTypeInfo();
            var typeMetadata = context.ServiceConfig.GetType<Metadata.ObjectTypeInfo>(typeInfo);
            type = context.Schema.GetType<ObjectType>(typeMetadata.Name);
        }

        var selectionSet = context.Operation.GetSelectionSet(selection, type);
        var result = context.Result.RentObject(selectionSet.Selections.Count);

        if (context.NeedsMoreData(selectionSet))
        {
            context.RegisterState(selectionSet, result, selectionResult);
        }
        else
        {
            var selections = selectionSet.Selections;
            var childSelectionResults = new SelectionResult[selections.Count];
            ExtractSelectionResults(selectionResult, selections, childSelectionResults);
            ComposeResult(context, selectionSet.Selections, childSelectionResults, result);
        }

        return result;
    }

    private static void ExtractSelectionResults(
        SelectionResult parent,
        IReadOnlyList<ISelection> selections,
        SelectionResult[] selectionResults)
    {
        if (parent.Multiple is null)
        {
            var schemaName = parent.Single.SubgraphName;
            var data = parent.Single.Element;

            for (var i = 0; i < selections.Count; i++)
            {
                if (data.TryGetProperty(selections[i].ResponseName, out var property))
                {
                    var current = selectionResults[i];

                    selectionResults[i] = current.HasValue
                        ? current.AddResult(new JsonResult(schemaName, property))
                        : new SelectionResult(new JsonResult(schemaName, property));
                }
            }
        }
        else
        {
            foreach (var result in parent.Multiple)
            {
                var schemaName = result.SubgraphName;
                var data = result.Element;

                for (var i = 0; i < selections.Count; i++)
                {
                    if (data.TryGetProperty(selections[i].ResponseName, out var property))
                    {
                        var current = selectionResults[i];

                        selectionResults[i] = current.HasValue
                            ? current.AddResult(new JsonResult(schemaName, property))
                            : new SelectionResult(new JsonResult(schemaName, property));
                    }
                }
            }
        }
    }

    public static void ExtractSelectionResults(
        IReadOnlyList<ISelection> selections,
        string schemaName,
        JsonElement data,
        SelectionResult[] selectionResults)
    {
        if (data.ValueKind is not JsonValueKind.Object)
        {
            return;
        }

        for (var i = 0; i < selections.Count; i++)
        {
            if (data.TryGetProperty(selections[i].ResponseName, out var property))
            {
                var selectionResult = selectionResults[i];

                if (selectionResult.HasValue)
                {
                    selectionResults[i] = selectionResult.AddResult(new(schemaName, property));
                }
                else
                {
                    selectionResults[i] = new(new JsonResult(schemaName, property));
                }
            }
        }
    }

    public static void ExtractPartialResult(WorkItem workItem)
    {
        // capture the partial result available
        var partialResult = workItem.SelectionResults[0];

        // if we have a partial result available lets unwrap it.
        if (partialResult.HasValue)
        {
            // first we need to erase the partial result from the array so that its not
            // combined into the result creation.
            workItem.SelectionResults[0] = default;

            // next we will unwrap the results.
            ExtractSelectionResults(
                partialResult,
                workItem.SelectionSet.Selections,
                workItem.SelectionResults);

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
        SelectionResult parent,
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
