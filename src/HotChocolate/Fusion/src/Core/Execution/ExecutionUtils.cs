using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Processing.Selection;
using IType = HotChocolate.Types.IType;
using ObjectType = HotChocolate.Types.ObjectType;

namespace HotChocolate.Fusion.Execution;

internal static class ExecutionUtils
{
    private const CustomOptionsFlags _reEncodeIdFlag =
        (CustomOptionsFlags)ObjectFieldFlags.ReEncodeId;

    private const CustomOptionsFlags _typeNameFlag =
        (CustomOptionsFlags)ObjectFieldFlags.TypeName;

    private static readonly ErrorPathVisitor _errorPathVisitor = new();

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
        bool partialResult = false,
        int level = 0)
    {
        if (selectionSetResult.IsInvalidated)
        {
            return;
        }

        var includeFlags = context.OperationContext.IncludeFlags;
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

            if (selection.IsConditional && !selection.IsIncluded(includeFlags))
            {
                goto NEXT_SELECTION;
            }

            if (!field.IsIntrospectionField)
            {
                var isSemanticNonNull = IsSemanticNonNull(selection, level);
                var nullable = selectionType.IsNullableType();
                var nullableType = selectionType.NullableType();

                if (!data.HasValue)
                {
                    if (!partialResult)
                    {
                        if (isSemanticNonNull)
                        {
                            AddSemanticNonNullViolation(context.Result, selection, selectionSetResult, responseIndex);
                        }
                        else if (!nullable)
                        {
                            PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                            break;
                        }

                        result.Set(responseName, null, nullable);
                    }
                }
                else if (nullableType.IsType(TypeKind.Scalar))
                {
                    var value = data.Single.Element;

                    if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    {
                        if (isSemanticNonNull)
                        {
                            AddSemanticNonNullViolation(context.Result, selection, selectionSetResult, responseIndex);
                        }
                        else if (!nullable)
                        {
                            PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                            break;
                        }
                    }

                    result.Set(responseName, value, nullable);

                    if (value.ValueKind is JsonValueKind.String
                        && (selection.CustomOptions & _reEncodeIdFlag) == _reEncodeIdFlag)
                    {
                        var subgraphName = data.Single.SubgraphName;
                        var reformattedId = context.ReformatId(value.GetString()!, subgraphName);
                        result.Set(responseName, reformattedId, nullable);
                    }
                }
                else if (nullableType.IsType(TypeKind.Enum))
                {
                    var value = data.Single.Element;

                    if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    {
                        if (isSemanticNonNull)
                        {
                            AddSemanticNonNullViolation(context.Result, selection, selectionSetResult, responseIndex);
                        }
                        else if (!nullable)
                        {
                            PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                            break;
                        }
                    }

                    result.Set(responseName, value, nullable);
                }
                else if (selectionType.IsCompositeType())
                {
                    if (!result.IsInitialized)
                    {
                        // we add a placeholder here so if ComposeObject propagates an error
                        // there is a value here.
                        result.Set(responseName, null, nullable);

                        var value = ComposeObject(
                            context,
                            selectionSetResult,
                            responseIndex,
                            selection,
                            data,
                            level);

                        if (value is null)
                        {
                            if (isSemanticNonNull)
                            {
                                AddSemanticNonNullViolation(context.Result, selection, selectionSetResult, responseIndex);
                            }
                            else if (!nullable)
                            {
                                PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                                break;
                            }
                        }

                        result.Set(responseName, value, nullable);
                    }
                }
                else
                {
                    if (!result.IsInitialized)
                    {
                        // we add a placeholder here so if ComposeList propagates an error
                        // there is a value here.
                        result.Set(responseName, null, nullable);

                        var value = ComposeList(
                            context,
                            selectionSetResult,
                            responseIndex,
                            selection,
                            data,
                            selectionType,
                            level + 1);

                        if (value is null)
                        {
                            if (isSemanticNonNull)
                            {
                                AddSemanticNonNullViolation(context.Result, selection, selectionSetResult, responseIndex);
                            }
                            else if (!nullable)
                            {
                                PropagateNullValues(context.Result, selection, selectionSetResult, responseIndex);
                                break;
                            }
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

            // move our pointers
            NEXT_SELECTION:
            responseIndex++;

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
        IType type,
        int level)
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
        var isSemanticNonNull = IsSemanticNonNull(selection, level);
        var result = context.Result.RentList(json.GetArrayLength());

        result.IsNullable = nullable;
        result.SetParent(parent, parentIndex);

        foreach (var item in json.EnumerateArray())
        {
            // we add a placeholder here so if ComposeElement propagates an error
            // there is a value here.
            result.AddUnsafe(null);

            var element = ComposeElement(
                context,
                result,
                index,
                selection,
                new SelectionData(new JsonResult(schemaName, item)),
                elementType,
                level);

            if (element is null)
            {
                if (isSemanticNonNull)
                {
                    AddSemanticNonNullViolation(context.Result, selection, result, index);
                }
                else if (!nullable)
                {
                    PropagateNullValues(context.Result, selection, result, index);
                    break;
                }
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
        IType valueType,
        int level)
    {
        var nullableType = valueType.NullableType();

        if (!selectionData.HasValue)
        {
            return null;
        }

        if (nullableType.IsType(TypeKind.Scalar))
        {
            var value = selectionData.Single.Element;

            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (value.ValueKind is JsonValueKind.String
                && (selection.CustomOptions & _reEncodeIdFlag) == _reEncodeIdFlag)
            {
                var subgraphName = selectionData.Single.SubgraphName;
                return context.ReformatId(value.GetString()!, subgraphName);
            }

            return value;
        }

        if (nullableType.IsType(TypeKind.Enum))
        {
            var value = selectionData.Single.Element;

            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            return value;
        }

        return nullableType.IsCompositeType()
            ? ComposeObject(context, parent, parentIndex, selection, selectionData, 0)
            : ComposeList(context, parent, parentIndex, selection, selectionData, valueType, level + 1);
    }

    private static ObjectResult? ComposeObject(
        FusionExecutionContext context,
        ResultData parent,
        int parentIndex,
        ISelection selection,
        SelectionData selectionData,
        int level)
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
            ComposeResult(context, selectionSet, childSelectionResults, result, true, level);
        }
        else
        {
            var childSelectionResults = new SelectionData[selectionCount];
            ExtractSelectionResults(selectionData, selectionSet, childSelectionResults);
            ComposeResult(context, selectionSet, childSelectionResults, result, false, level);
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
                if (data.ValueKind is not JsonValueKind.Null
                    && data.TryGetProperty(selection.ResponseName, out var value))
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
                    if (element.ValueKind is not JsonValueKind.Null
                        && element.TryGetProperty(selection.ResponseName, out var value))
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

    public static void CreateTransportErrors(
        Exception transportException,
        ResultBuilder resultBuilder,
        IErrorHandler errorHandler,
        ObjectResult selectionSetResult,
        List<RootSelection> rootSelections,
        string subgraphName,
        bool addDebugInfo)
    {
        foreach (var rootSelection in rootSelections)
        {
            var errorBuilder = errorHandler.CreateUnexpectedError(transportException);

            errorBuilder.AddLocation(rootSelection.Selection.SyntaxNode);
            errorBuilder.SetPath(PathHelper.CreatePathFromContext(rootSelection.Selection, selectionSetResult, 0));

            if (addDebugInfo)
            {
                errorBuilder.SetExtension("subgraphName", subgraphName);
            }

            var error = errorHandler.Handle(errorBuilder.Build());

            resultBuilder.AddError(error);
        }
    }

    public static void ExtractErrors(
        DocumentNode document,
        OperationDefinitionNode operation,
        ResultBuilder resultBuilder,
        IErrorHandler errorHandler,
        JsonElement errors,
        ObjectResult selectionSetResult,
        int pathDepth,
        bool addDebugInfo)
    {
        if (errors.ValueKind is not JsonValueKind.Array)
        {
            return;
        }

        var parentPath = PathHelper.CreatePathFromContext(selectionSetResult);

        foreach (var error in errors.EnumerateArray())
        {
            ExtractError(document, operation, resultBuilder, errorHandler, error, parentPath, pathDepth, addDebugInfo);
        }
    }

    private static void ExtractError(
        DocumentNode document,
        OperationDefinitionNode operation,
        ResultBuilder resultBuilder,
        IErrorHandler errorHandler,
        JsonElement error,
        Path parentPath,
        int pathDepth,
        bool addDebugInfo)
    {
        FieldNode? field = null;

        if (error.ValueKind is not JsonValueKind.Object)
        {
            return;
        }

        if (error.TryGetProperty("message", out var message) && message.ValueKind is JsonValueKind.String)
        {
            var errorBuilder = new ErrorBuilder();
            errorBuilder.SetMessage(message.GetString()!);

            if (error.TryGetProperty("code", out var code) && code.ValueKind is JsonValueKind.String)
            {
                errorBuilder.SetCode(code.GetString());
            }

            if (error.TryGetProperty("extensions", out var extensions) && extensions.ValueKind is JsonValueKind.Object)
            {
                foreach (var property in extensions.EnumerateObject())
                {
                    errorBuilder.SetExtension(property.Name, property.Value);
                }
            }

            if (error.TryGetProperty("path", out var remotePath) && remotePath.ValueKind is JsonValueKind.Array)
            {
                var path = PathHelper.CombinePath(parentPath, remotePath, pathDepth);
                errorBuilder.SetPath(path);

                field = _errorPathVisitor.GetFieldForPath(document, operation, path);

                if (addDebugInfo)
                {
                    errorBuilder.SetExtension("remotePath", remotePath);
                }
            }

            if (field is null
                && error.TryGetProperty("locations", out var locations)
                && locations.ValueKind is JsonValueKind.Array)
            {
                foreach (var location in locations.EnumerateArray())
                {
                    if (location.TryGetProperty("line", out var lineValue)
                        && location.TryGetProperty("column", out var columnValue)
                        && lineValue.TryGetInt32(out var line)
                        && columnValue.TryGetInt32(out var column))
                    {
                        errorBuilder.AddLocation(line, column);
                    }
                }
            }

            if (field is not null)
            {
                errorBuilder.AddLocation(field);
            }

            var handledError = errorHandler.Handle(errorBuilder.Build());

            resultBuilder.AddError(handledError);
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

        if (exportKeys.Count > 0 && parent.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            for (var i = 0; i < exportKeys.Count; i++)
            {
                var key = exportKeys[i];

                if (!variableValues.ContainsKey(key) && parent.TryGetProperty(key, out var property))
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

    private static void AddSemanticNonNullViolation(
        ResultBuilder resultBuilder,
        Selection selection,
        ResultData selectionSetResult,
        int responseIndex)
    {
        var path = PathHelper.CreatePathFromContext(selection, selectionSetResult, responseIndex);
        var error = SemanticNonNullTypeInterceptor.CreateSemanticNonNullViolationError(path, selection);
        resultBuilder.AddError(error);
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

    // TODO: Pull out
    private const int MaxLevels = 3;

    private static readonly CustomOptionsFlags[] _levelOptions =
    [
        CustomOptionsFlags.Option5,
        CustomOptionsFlags.Option6,
        CustomOptionsFlags.Option7
    ];

    private static bool IsSemanticNonNull(Selection selection, int level)
    {
        if (level >= MaxLevels)
        {
            return true;
        }

        var optionForLevel = _levelOptions[level];

        return selection.CustomOptions.HasFlag(optionForLevel);
    }

    private sealed class ErrorPathContext
    {
        public string Current { get; set; } = string.Empty;

        public Stack<string> Path { get; } = new();

        public Dictionary<string, FragmentDefinitionNode> Fragments { get; } = new();

        public FieldNode? Field { get; set; }

        public void Reset()
        {
            Path.Clear();
            Fragments.Clear();
            Current = string.Empty;
            Field = null;
        }
    }

    private sealed class ErrorPathVisitor : SyntaxWalker<ErrorPathContext>
    {
        private static ErrorPathContext? _errorPathContext = null;

        public FieldNode? GetFieldForPath(
            DocumentNode document,
            OperationDefinitionNode operation,
            Path path)
        {
            if (path.IsRoot)
            {
                return null;
            }

            var context = Interlocked.Exchange(ref _errorPathContext, null) ?? new ErrorPathContext();

            InitializePath(path, context.Path);
            InitializeFragments(document, context.Fragments);

            if (context.Path.Count > 0)
            {
                context.Current = context.Path.Pop();
            }

            Visit(operation, context);

            var field = context.Field;

            context.Reset();
            Interlocked.Exchange(ref _errorPathContext, context);

            return field;
        }

        private static void InitializeFragments(
            DocumentNode document,
            Dictionary<string, FragmentDefinitionNode> fragments)
        {
            foreach (var definition in document.Definitions)
            {
                if (definition is FragmentDefinitionNode fragment)
                {
                    fragments.TryAdd(fragment.Name.Value, fragment);
                }
            }
        }

        private static void InitializePath(
            Path path,
            Stack<string> pathStack)
        {
            while (!path.IsRoot)
            {
                if (path is NamePathSegment namePath)
                {
                    pathStack.Push(namePath.Name);
                }

                path = path.Parent;
            }
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            ErrorPathContext context)
        {
            if (context.Current.EqualsOrdinal(node.Name.Value))
            {
                if (context.Path.Count == 0)
                {
                    context.Field = node;
                    return Break;
                }

                context.Current = context.Path.Pop();
                return base.Enter(node, context);
            }

            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            FragmentSpreadNode node,
            ErrorPathContext context)
        {
            if (base.VisitChildren(node, context).IsBreak())
            {
                return Break;
            }

            if (context.Fragments.TryGetValue(node.Name.Value, out var fragment))
            {
                if (Visit(fragment, node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }
    }
}
