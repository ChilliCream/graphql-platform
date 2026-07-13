using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore
{
    private const string RepresentationsVariableName = "representations";
    private const string TypeNameFieldName = "__typename";
    private static ReadOnlySpan<byte> TypeNameFieldNameUtf8 => "__typename"u8;

    private static readonly ArrayPool<EntityResultPath> s_entityResultPathPool =
        ArrayPool<EntityResultPath>.Shared;

    public bool AddRepresentationResult(
        SelectionPath sourcePath,
        SourceSchemaResult result,
        RepresentationValue representation,
        ResultSelectionSet resultSelectionSet,
        bool containsErrors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(result);

        var dataElement = GetDataElement(sourcePath, result.Data);
        var errors = containsErrors ? result.Errors : null;
        var errorTrie = containsErrors ? GetErrorTrie(sourcePath, errors?.Trie) : null;
        var resultPaths = representation.ResultPaths.AsSpan();

        lock (_lock)
        {
            _memory.Add(result);

            try
            {
                var hasSourceErrors = errors is not null
                    && (!errors.RootErrors.IsDefaultOrEmpty
                        || errorTrie?.FindFirstError() is not null);

                if (dataElement.ValueKind is not JsonValueKind.Array)
                {
                    if (hasSourceErrors)
                    {
                        return SaveRepresentationSourceErrors(
                            _result.Data,
                            errors!,
                            errorTrie,
                            resultPaths,
                            resultSelectionSet);
                    }

                    return SaveRepresentationProtocolError(
                        _result.Data,
                        ThrowHelper.InvalidRepresentationResultKind(sourcePath, dataElement.ValueKind),
                        resultPaths,
                        resultSelectionSet);
                }

                var resultCount = dataElement.GetArrayLength();
                if (resultPaths.IsEmpty || resultCount != resultPaths.Length)
                {
                    if (hasSourceErrors
                        && !SaveRepresentationSourceErrors(
                            _result.Data,
                            errors!,
                            errorTrie,
                            resultPaths,
                            resultSelectionSet))
                    {
                        return false;
                    }

                    return SaveRepresentationProtocolError(
                        _result.Data,
                        ThrowHelper.RepresentationResultCountMismatch(
                            sourcePath,
                            resultCount,
                            resultPaths.Length),
                        resultPaths,
                        resultSelectionSet);
                }

                if (errors?.RootErrors is { Length: > 0 } rootErrors)
                {
                    _errors ??= [];

                    foreach (var rootError in rootErrors)
                    {
                        _errors.Add(_errorHandler.Handle(rootError));
                    }
                }

                return SaveRepresentationResult(
                    _result.Data,
                    dataElement,
                    errorTrie,
                    resultPaths,
                    resultSelectionSet);
            }
            finally
            {
                ReturnRepresentationPathSegments(representation);
                ReturnPathSegments(result);
            }
        }
    }

    public RepresentationValue CreateRepresentationVariableValue(
        SelectionPath selectionSet,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData,
        string entityTypeName,
        List<RepresentationShapeNode> shape)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(selectionSet);
        ArgumentNullException.ThrowIfNull(requestVariables);
        ArgumentNullException.ThrowIfNull(shape);

        if (requiredData.Length == 0)
        {
            return RepresentationValue.Empty;
        }

        lock (_lock)
        {
            var elements = CollectTargetElements(selectionSet);

            if (elements.IsEmpty)
            {
                return RepresentationValue.Empty;
            }

            return BuildRepresentationValue(
                elements,
                requestVariables,
                entityTypeName,
                shape);
        }
    }

    internal RepresentationValue CreateRepresentationVariableValueFromSnapshot(
        ImmutableArray<VariableValues> importedEntries,
        HashSet<string> importedKeys,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData,
        string entityTypeName,
        List<RepresentationShapeNode> shape)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(importedKeys);
        ArgumentNullException.ThrowIfNull(requestVariables);
        ArgumentNullException.ThrowIfNull(shape);

        if (importedEntries.IsDefaultOrEmpty || requiredData.Length == 0)
        {
            return RepresentationValue.Empty;
        }

        foreach (var requirement in requiredData)
        {
            if (!importedKeys.Contains(requirement.Key))
            {
                throw new InvalidOperationException(
                    "A deferred incremental plan fetch references a requirement that was not imported.");
            }
        }

        lock (_lock)
        {
            return BuildRepresentationValueFromSnapshot(
                importedEntries,
                requestVariables,
                requiredData,
                entityTypeName,
                shape);
        }
    }

    private RepresentationValue BuildRepresentationValueFromSnapshot(
        ImmutableArray<VariableValues> importedEntries,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData,
        string entityTypeName,
        List<RepresentationShapeNode> shape)
    {
        Span<(long Start, long Length)> requirementSlices = requiredData.Length <= 32
            ? stackalloc (long, long)[requiredData.Length]
            : new (long, long)[requiredData.Length];
        using var representationWriter = new ChunkedArrayWriter();
        using var dedupTable = new VariableDedupTable(representationWriter);
        var representationJsonWriter = new JsonWriter(
            representationWriter,
            new JsonWriterOptions { Indented = false });
        var resultPaths = s_entityResultPathPool.Rent(importedEntries.Length);
        var additionalPaths = new RepresentationPathAccumulator(importedEntries.Length);
        var completed = false;
        var variableStartPosition = StartRepresentationVariableValue();
        var nextIndex = 0;

        dedupTable.Initialize(importedEntries.Length);

        try
        {
            foreach (var importedEntry in importedEntries)
            {
                if (importedEntry.IsEmpty)
                {
                    continue;
                }

                var writeResult = TryWriteRepresentation(
                    importedEntry.Values,
                    requiredData,
                    shape,
                    entityTypeName,
                    requirementSlices,
                    representationWriter,
                    representationJsonWriter,
                    dedupTable,
                    nextIndex,
                    out var index);

                switch (writeResult)
                {
                    case RepresentationWriteResult.Skipped:
                        break;

                    case RepresentationWriteResult.Duplicate:
                        additionalPaths.Add(index, importedEntry.Path);
                        additionalPaths.AddRange(index, importedEntry.AdditionalPaths.AsSpan());
                        break;

                    case RepresentationWriteResult.Written:
                        resultPaths[nextIndex] = new EntityResultPath(importedEntry.Path, default);
                        additionalPaths.AddRange(nextIndex, importedEntry.AdditionalPaths.AsSpan());
                        nextIndex++;
                        break;
                }
            }

            var value = CompleteRepresentationVariableValue(
                variableStartPosition,
                requestVariables,
                resultPaths,
                ref additionalPaths,
                nextIndex);
            completed = true;
            resultPaths = null;
            return value;
        }
        finally
        {
            if (!completed)
            {
                _variableWriter.ResetTo(variableStartPosition);
                additionalPaths.Dispose();

                if (resultPaths is not null)
                {
                    s_entityResultPathPool.Return(resultPaths, clearArray: true);
                }
            }
        }
    }

    private RepresentationValue BuildRepresentationValue(
        ReadOnlySpan<CompositeResultElement> elements,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        string entityTypeName,
        List<RepresentationShapeNode> shape)
    {
        using var representationWriter = new ChunkedArrayWriter();
        using var dedupTable = new VariableDedupTable(representationWriter);
        var representationJsonWriter = new JsonWriter(
            representationWriter,
            new JsonWriterOptions { Indented = false });
        var resultPaths = s_entityResultPathPool.Rent(elements.Length);
        var additionalPaths = new RepresentationPathAccumulator(elements.Length);
        var completed = false;
        var variableStartPosition = StartRepresentationVariableValue();
        var nextIndex = 0;

        dedupTable.Initialize(elements.Length);

        try
        {
            foreach (var result in elements)
            {
                var writeResult = TryWriteRepresentation(
                    result,
                    shape,
                    entityTypeName,
                    representationWriter,
                    representationJsonWriter,
                    dedupTable,
                    nextIndex,
                    out var index);

                switch (writeResult)
                {
                    case RepresentationWriteResult.Skipped:
                        break;

                    case RepresentationWriteResult.Duplicate:
                        additionalPaths.Add(index, result.CompactPath);
                        break;

                    case RepresentationWriteResult.Written:
                        resultPaths[nextIndex] = new EntityResultPath(result.CompactPath, default);
                        nextIndex++;
                        break;
                }
            }

            var value = CompleteRepresentationVariableValue(
                variableStartPosition,
                requestVariables,
                resultPaths,
                ref additionalPaths,
                nextIndex);
            completed = true;
            resultPaths = null!;
            return value;
        }
        finally
        {
            if (!completed)
            {
                _variableWriter.ResetTo(variableStartPosition);
                additionalPaths.Dispose();

                if (resultPaths is not null)
                {
                    s_entityResultPathPool.Return(resultPaths, clearArray: true);
                }
            }
        }
    }

    private void WriteRequestVariableProperties(IReadOnlyList<ObjectFieldNode> requestVariables)
    {
        for (var i = 0; i < requestVariables.Count; i++)
        {
            var field = requestVariables[i];
            _jsonWriter.WritePropertyName(field.Name.Value);
            WriteValueNode(field.Value);
        }
    }

    private int StartRepresentationVariableValue()
    {
        _jsonWriter.Reset(_variableWriter);
        var startPosition = _variableWriter.Position;
        _jsonWriter.WriteStartObject();
        _jsonWriter.WritePropertyName(RepresentationsVariableName);
        _jsonWriter.WriteStartArray();
        return startPosition;
    }

    private RepresentationValue CompleteRepresentationVariableValue(
        int startPosition,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        EntityResultPath[] resultPaths,
        ref RepresentationPathAccumulator additionalPaths,
        int count)
    {
        if (count == 0)
        {
            _variableWriter.ResetTo(startPosition);
            additionalPaths.Dispose();
            s_entityResultPathPool.Return(resultPaths, clearArray: true);
            return RepresentationValue.Empty;
        }

        _jsonWriter.WriteEndArray();
        WriteRequestVariableProperties(requestVariables);
        _jsonWriter.WriteEndObject();

        additionalPaths.ApplyTo(resultPaths, count);
        additionalPaths.Dispose();

        var length = _variableWriter.Position - startPosition;
        var value = JsonSegment.Create(_variableWriter, startPosition, length);
        var resultPathSpan = resultPaths.AsSpan(0, count);
        var resultPathArray = resultPathSpan.ToArray();

        resultPathSpan.Clear();
        s_entityResultPathPool.Return(resultPaths, clearArray: true);

        return new RepresentationValue(
            value,
            ImmutableCollectionsMarshal.AsImmutableArray(resultPathArray));
    }

    private RepresentationWriteResult TryWriteRepresentation(
        CompositeResultElement result,
        List<RepresentationShapeNode> shape,
        string entityTypeName,
        ChunkedArrayWriter representationWriter,
        JsonWriter representationJsonWriter,
        VariableDedupTable dedupTable,
        int nextIndex,
        out int index)
    {
        if (!TryBufferRepresentation(
            result,
            shape,
            entityTypeName,
            representationWriter,
            representationJsonWriter,
            out var startPosition))
        {
            index = -1;
            return RepresentationWriteResult.Skipped;
        }

        return TryAppendRepresentation(
            representationWriter,
            startPosition,
            dedupTable,
            nextIndex,
            out index);
    }

    private bool TryBufferRepresentation(
        CompositeResultElement result,
        List<RepresentationShapeNode> shape,
        string entityTypeName,
        ChunkedArrayWriter representationWriter,
        JsonWriter representationJsonWriter,
        out int startPosition)
    {
        startPosition = representationWriter.Position;
        representationJsonWriter.Reset(representationWriter);
        representationJsonWriter.WriteStartObject();
        representationJsonWriter.WritePropertyName(TypeNameFieldName);
        representationJsonWriter.WriteStringValue(entityTypeName);

        if (!TryWriteShapeLevel(result, shape, representationJsonWriter))
        {
            representationWriter.ResetTo(startPosition);
            return false;
        }

        representationJsonWriter.WriteEndObject();
        return true;
    }

    private RepresentationWriteResult TryWriteRepresentation(
        JsonSegment values,
        ReadOnlySpan<OperationRequirement> requiredData,
        List<RepresentationShapeNode> shape,
        string entityTypeName,
        Span<(long Start, long Length)> requirementSlices,
        ChunkedArrayWriter representationWriter,
        JsonWriter representationJsonWriter,
        VariableDedupTable dedupTable,
        int nextIndex,
        out int index)
    {
        if (!TryBufferRepresentationFromSnapshot(
            values,
            requiredData,
            shape,
            entityTypeName,
            requirementSlices,
            representationWriter,
            representationJsonWriter,
            out var startPosition))
        {
            index = -1;
            return RepresentationWriteResult.Skipped;
        }

        return TryAppendRepresentation(
            representationWriter,
            startPosition,
            dedupTable,
            nextIndex,
            out index);
    }

    private bool TryBufferRepresentationFromSnapshot(
        JsonSegment values,
        ReadOnlySpan<OperationRequirement> requiredData,
        List<RepresentationShapeNode> shape,
        string entityTypeName,
        Span<(long Start, long Length)> requirementSlices,
        ChunkedArrayWriter representationWriter,
        JsonWriter representationJsonWriter,
        out int startPosition)
    {
        if (values.IsEmpty)
        {
            startPosition = -1;
            return false;
        }

        startPosition = representationWriter.Position;
        representationJsonWriter.Reset(representationWriter);
        representationJsonWriter.WriteStartObject();
        representationJsonWriter.WritePropertyName(TypeNameFieldName);
        representationJsonWriter.WriteStringValue(entityTypeName);

        var sequence = values.AsSequence();

        if (!TryCollectRequirementSlices(sequence, requiredData, requirementSlices)
            || !TryWriteShapeLevelFromSnapshot(sequence, requirementSlices, shape, representationJsonWriter))
        {
            representationWriter.ResetTo(startPosition);
            return false;
        }

        representationJsonWriter.WriteEndObject();
        return true;
    }

    private RepresentationWriteResult TryAppendRepresentation(
        ChunkedArrayWriter representationWriter,
        int startPosition,
        VariableDedupTable dedupTable,
        int nextIndex,
        out int index)
    {
        var writeResult = TryDeduplicateRepresentation(
            representationWriter,
            startPosition,
            dedupTable,
            nextIndex,
            out index);

        if (writeResult is RepresentationWriteResult.Written)
        {
            var length = representationWriter.Position - startPosition;
            JsonSegment.Create(representationWriter, startPosition, length).WriteTo(_jsonWriter);
        }

        return writeResult;
    }

    private static RepresentationWriteResult TryDeduplicateRepresentation(
        ChunkedArrayWriter representationWriter,
        int startPosition,
        VariableDedupTable dedupTable,
        int nextIndex,
        out int index)
    {
        var length = representationWriter.Position - startPosition;
        var hash = representationWriter.GetHashCode(startPosition, length);

        if (dedupTable.TryGet(hash, startPosition, length, out var existingIndex))
        {
            representationWriter.ResetTo(startPosition);
            index = existingIndex;
            return RepresentationWriteResult.Duplicate;
        }

        dedupTable.Add(hash, nextIndex, startPosition, length);
        index = nextIndex;
        return RepresentationWriteResult.Written;
    }

    private bool TryWriteShapeLevel(
        CompositeResultElement element,
        List<RepresentationShapeNode> level,
        JsonWriter writer)
    {
        for (var i = 0; i < level.Count; i++)
        {
            var node = level[i];

            if (node.ParentTypeCondition is not null
                && !SatisfiesTypeCondition(element, node.ParentTypeCondition))
            {
                return false;
            }

            if (!element.TryGetProperty(node.ResponseNameUtf8, out var value))
            {
                return false;
            }

            var valueKind = value.ValueKind;

            if (valueKind is JsonValueKind.Undefined)
            {
                return false;
            }

            if (node.TypeCondition is not null
                && valueKind is not JsonValueKind.Null
                && (valueKind is not JsonValueKind.Object
                    || !SatisfiesTypeCondition(value, node.TypeCondition)))
            {
                return false;
            }

            writer.WritePropertyName(node.NameUtf8);

            if (valueKind is JsonValueKind.Null)
            {
                if (node.SkipOnNull)
                {
                    return false;
                }

                if (node.Children is null || node.IsList)
                {
                    writer.WriteNullValue();
                    continue;
                }

                if (!TryWriteNullLeafStructure(node.Children, writer))
                {
                    return false;
                }

                continue;
            }

            if (node.Children is null)
            {
                if (valueKind is JsonValueKind.Array)
                {
                    if (!TryWriteLeafArray(value, node.ElementInputType, writer))
                    {
                        return false;
                    }
                }
                else
                {
                    WriteLeafValue(value, valueKind, writer);
                }

                continue;
            }

            if (valueKind is JsonValueKind.Object)
            {
                writer.WriteStartObject();

                if (node.Branches is { Count: > 0 })
                {
                    if (!TryWriteBranchedComposite(value, node, writer))
                    {
                        return false;
                    }
                }
                else
                {
                    if (node.RequiresTypeName)
                    {
                        if (!TryResolveRuntimeType(value, out var runtimeTypeName, out _))
                        {
                            return false;
                        }

                        writer.WritePropertyName(TypeNameFieldName);
                        writer.WriteStringValue(runtimeTypeName);
                    }

                    if (!TryWriteShapeLevel(value, node.Children, writer))
                    {
                        return false;
                    }
                }

                writer.WriteEndObject();
                continue;
            }

            if (valueKind is JsonValueKind.Array)
            {
                if (!TryWriteShapeElementArray(value, node, node.ElementInputType, writer))
                {
                    return false;
                }

                continue;
            }

            return false;
        }

        return true;
    }

    private bool TryWriteBranchedComposite(
        CompositeResultElement value,
        RepresentationShapeNode node,
        JsonWriter writer)
    {
        if (!TryResolveRuntimeType(value, out var runtimeTypeName, out var runtimeType))
        {
            return false;
        }

        writer.WritePropertyName(TypeNameFieldName);
        writer.WriteStringValue(runtimeTypeName);

        if (!TryWriteShapeLevel(value, node.Children!, writer))
        {
            return false;
        }

        var branches = node.Branches!;

        for (var i = 0; i < branches.Count; i++)
        {
            var branch = branches[i];
            var branchType = _schema.Types.GetType<IOutputTypeDefinition>(branch.TypeCondition);

            if (branchType.IsAssignableFrom(runtimeType)
                && !TryWriteShapeLevel(value, branch.Children, writer))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryWriteShapeElementArray(
        CompositeResultElement array,
        RepresentationShapeNode node,
        ITypeNode? elementType,
        JsonWriter writer)
    {
        writer.WriteStartArray();

        foreach (var item in array.EnumerateArray())
        {
            var itemKind = item.ValueKind;

            if (itemKind is JsonValueKind.Null)
            {
                if (IsNonNullPosition(elementType))
                {
                    return false;
                }

                writer.WriteNullValue();
                continue;
            }

            if (itemKind is JsonValueKind.Array)
            {
                if (!TryWriteShapeElementArray(item, node, GetElementType(elementType), writer))
                {
                    return false;
                }

                continue;
            }

            if (itemKind is not JsonValueKind.Object)
            {
                return false;
            }

            writer.WriteStartObject();

            if (!TryWriteShapeLevel(item, node.Children!, writer))
            {
                return false;
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        return true;
    }

    private static bool TryWriteNullLeafStructure(
        List<RepresentationShapeNode> level,
        JsonWriter writer)
    {
        writer.WriteStartObject();

        for (var i = 0; i < level.Count; i++)
        {
            var node = level[i];

            if (node.SkipOnNull)
            {
                return false;
            }

            writer.WritePropertyName(node.NameUtf8);

            if (node.Children is null || node.IsList)
            {
                writer.WriteNullValue();
            }
            else if (!TryWriteNullLeafStructure(node.Children, writer))
            {
                return false;
            }
        }

        writer.WriteEndObject();
        return true;
    }

    private static bool TryWriteLeafArray(
        CompositeResultElement array,
        ITypeNode? elementType,
        JsonWriter writer)
    {
        writer.WriteStartArray();

        foreach (var item in array.EnumerateArray())
        {
            var itemKind = item.ValueKind;

            if (itemKind is JsonValueKind.Null)
            {
                if (IsNonNullPosition(elementType))
                {
                    return false;
                }

                writer.WriteNullValue();
            }
            else if (itemKind is JsonValueKind.Array)
            {
                if (!TryWriteLeafArray(item, GetElementType(elementType), writer))
                {
                    return false;
                }
            }
            else
            {
                WriteLeafValue(item, itemKind, writer);
            }
        }

        writer.WriteEndArray();
        return true;
    }

    private static void WriteLeafValue(
        CompositeResultElement value,
        JsonValueKind valueKind,
        JsonWriter writer)
    {
        // A custom scalar can have a JSON object as its runtime value, which
        // only the backing document can serialize.
        if (valueKind is JsonValueKind.Object)
        {
            value.WriteTo(writer);
            return;
        }

        writer.WriteRawValue(value.GetRawValue(includeQuotes: true));
    }

    private static bool IsNonNullPosition(ITypeNode? type)
        => type?.Kind is SyntaxKind.NonNullType;

    private static ITypeNode? GetElementType(ITypeNode? type)
        => type?.IsListType() == true ? type.ElementType() : null;

    private bool SatisfiesTypeCondition(CompositeResultElement element, string typeName)
    {
        var type = _schema.Types.GetType<IOutputTypeDefinition>(typeName);
        return type.IsAssignableFrom(element.AssertSelectionSet().Type);
    }

    private bool TryResolveRuntimeType(
        CompositeResultElement element,
        out string runtimeTypeName,
        out IOutputTypeDefinition runtimeType)
    {
        runtimeTypeName = null!;
        runtimeType = null!;

        if (!element.TryGetProperty(TypeNameFieldNameUtf8, out var typeNameElement)
            || typeNameElement.ValueKind is not JsonValueKind.String
            || typeNameElement.GetString() is not { } typeName)
        {
            return false;
        }

        runtimeTypeName = typeName;
        runtimeType = _schema.Types.GetType<IOutputTypeDefinition>(typeName);
        return true;
    }

    private static bool TryCollectRequirementSlices(
        ReadOnlySequence<byte> values,
        ReadOnlySpan<OperationRequirement> requiredData,
        Span<(long Start, long Length)> slices)
    {
        slices.Fill((-1, 0));

        var reader = new Utf8JsonReader(values);

        if (!reader.Read() || reader.TokenType is not JsonTokenType.StartObject)
        {
            return false;
        }

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                return true;
            }

            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                return false;
            }

            var index = -1;

            for (var i = 0; i < requiredData.Length; i++)
            {
                if (reader.ValueTextEquals(requiredData[i].Key))
                {
                    index = i;
                    break;
                }
            }

            if (!reader.Read())
            {
                return false;
            }

            var start = reader.TokenStartIndex;
            reader.Skip();
            var length = reader.BytesConsumed - start;

            if (index >= 0)
            {
                slices[index] = (start, length);
            }
        }

        return false;
    }

    private bool TryWriteShapeLevelFromSnapshot(
        ReadOnlySequence<byte> values,
        ReadOnlySpan<(long Start, long Length)> slices,
        List<RepresentationShapeNode> level,
        JsonWriter writer)
    {
        for (var i = 0; i < level.Count; i++)
        {
            var node = level[i];

            if (node.Children is not null && !node.IsList)
            {
                writer.WritePropertyName(node.NameUtf8);
                writer.WriteStartObject();

                if (node.Branches is { Count: > 0 })
                {
                    if (!TryWriteBranchedCompositeFromSnapshot(values, slices, node, writer))
                    {
                        return false;
                    }
                }
                else
                {
                    if (node.RequiresTypeName)
                    {
                        if (!TryResolveRuntimeTypeFromSnapshot(
                            values,
                            slices,
                            node.Children,
                            out var runtimeTypeName,
                            out _))
                        {
                            return false;
                        }

                        writer.WritePropertyName(TypeNameFieldName);
                        writer.WriteStringValue(runtimeTypeName);
                    }

                    if (!TryWriteShapeLevelFromSnapshot(values, slices, node.Children, writer))
                    {
                        return false;
                    }
                }

                writer.WriteEndObject();
                continue;
            }

            var (start, length) = slices[node.RequirementIndex];

            if (start < 0)
            {
                return false;
            }

            if (!TryWriteNodeValueFromScope(values.Slice(start, length), node, writer))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryWriteBranchedCompositeFromSnapshot(
        ReadOnlySequence<byte> values,
        ReadOnlySpan<(long Start, long Length)> slices,
        RepresentationShapeNode node,
        JsonWriter writer)
    {
        if (!TryResolveRuntimeTypeFromSnapshot(
            values,
            slices,
            node,
            out var runtimeTypeName,
            out var runtimeType))
        {
            return false;
        }

        writer.WritePropertyName(TypeNameFieldName);
        writer.WriteStringValue(runtimeTypeName);

        if (!TryWriteShapeLevelFromSnapshot(values, slices, node.Children!, writer))
        {
            return false;
        }

        var branches = node.Branches!;

        for (var i = 0; i < branches.Count; i++)
        {
            var branch = branches[i];
            var branchType = _schema.Types.GetType<IOutputTypeDefinition>(branch.TypeCondition);

            if (branchType.IsAssignableFrom(runtimeType)
                && !TryWriteShapeLevelFromSnapshot(values, slices, branch.Children, writer))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryWriteNodesFromScope(
        ReadOnlySequence<byte> scope,
        List<RepresentationShapeNode> nodes,
        JsonWriter writer)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];

            if (node.Children is not null && !node.IsList)
            {
                writer.WritePropertyName(node.NameUtf8);
                writer.WriteStartObject();

                if (node.Branches is { Count: > 0 })
                {
                    if (!TryWriteBranchedCompositeFromScope(scope, node, writer))
                    {
                        return false;
                    }
                }
                else
                {
                    if (node.RequiresTypeName)
                    {
                        if (!TryResolveRuntimeTypeFromSnapshotScope(
                            scope,
                            out var runtimeTypeName,
                            out _))
                        {
                            return false;
                        }

                        writer.WritePropertyName(TypeNameFieldName);
                        writer.WriteStringValue(runtimeTypeName);
                    }

                    if (!TryWriteNodesFromScope(scope, node.Children, writer))
                    {
                        return false;
                    }
                }

                writer.WriteEndObject();
                continue;
            }

            if (!TryWriteNodeValueFromScope(scope, node, writer))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryWriteBranchedCompositeFromScope(
        ReadOnlySequence<byte> scope,
        RepresentationShapeNode node,
        JsonWriter writer)
    {
        if (!TryResolveRuntimeTypeFromSnapshotScope(
            scope,
            out var runtimeTypeName,
            out var runtimeType))
        {
            return false;
        }

        writer.WritePropertyName(TypeNameFieldName);
        writer.WriteStringValue(runtimeTypeName);

        if (!TryWriteNodesFromScope(scope, node.Children!, writer))
        {
            return false;
        }

        var branches = node.Branches!;

        for (var i = 0; i < branches.Count; i++)
        {
            var branch = branches[i];
            var branchType = _schema.Types.GetType<IOutputTypeDefinition>(branch.TypeCondition);

            if (branchType.IsAssignableFrom(runtimeType)
                && !TryWriteNodesFromScope(scope, branch.Children, writer))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryWriteNodeValueFromScope(
        ReadOnlySequence<byte> scope,
        RepresentationShapeNode node,
        JsonWriter writer)
    {
        var value = scope;

        for (var i = 0; i < node.LhsPath.Length; i++)
        {
            if (!TryResolveProperty(value, node.LhsPath[i], out value))
            {
                return false;
            }
        }

        writer.WritePropertyName(node.NameUtf8);

        if (node.IsList)
        {
            return TryWriteListFromScope(value, node.Children!, writer);
        }

        WriteRawJsonValue(writer, value);
        return true;
    }

    private bool TryWriteListFromScope(
        ReadOnlySequence<byte> value,
        List<RepresentationShapeNode> children,
        JsonWriter writer)
    {
        var reader = new Utf8JsonReader(value);

        if (!reader.Read())
        {
            return false;
        }

        if (reader.TokenType is JsonTokenType.Null)
        {
            writer.WriteNullValue();
            return true;
        }

        if (reader.TokenType is not JsonTokenType.StartArray)
        {
            return false;
        }

        writer.WriteStartArray();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    return true;

                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    continue;

                case JsonTokenType.StartObject:
                    var start = reader.TokenStartIndex;
                    reader.Skip();
                    var length = reader.BytesConsumed - start;

                    writer.WriteStartObject();

                    if (!TryWriteNodesFromScope(value.Slice(start, length), children, writer))
                    {
                        return false;
                    }

                    writer.WriteEndObject();
                    continue;

                default:
                    return false;
            }
        }

        return false;
    }

    private bool TryResolveRuntimeTypeFromSnapshot(
        ReadOnlySequence<byte> values,
        ReadOnlySpan<(long Start, long Length)> slices,
        RepresentationShapeNode node,
        out string runtimeTypeName,
        out IOutputTypeDefinition runtimeType)
    {
        if (TryResolveRuntimeTypeFromSnapshot(
            values,
            slices,
            node.Children!,
            out runtimeTypeName,
            out runtimeType))
        {
            return true;
        }

        var branches = node.Branches!;

        for (var i = 0; i < branches.Count; i++)
        {
            if (TryResolveRuntimeTypeFromSnapshot(
                values,
                slices,
                branches[i].Children,
                out runtimeTypeName,
                out runtimeType))
            {
                return true;
            }
        }

        runtimeTypeName = null!;
        runtimeType = null!;
        return false;
    }

    private bool TryResolveRuntimeTypeFromSnapshot(
        ReadOnlySequence<byte> values,
        ReadOnlySpan<(long Start, long Length)> slices,
        List<RepresentationShapeNode> nodes,
        out string runtimeTypeName,
        out IOutputTypeDefinition runtimeType)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];

            if (node.Children is not null && !node.IsList)
            {
                if (TryResolveRuntimeTypeFromSnapshot(
                    values,
                    slices,
                    node.Children,
                    out runtimeTypeName,
                    out runtimeType))
                {
                    return true;
                }

                continue;
            }

            var requirementIndex = node.RequirementIndex;

            if ((uint)requirementIndex >= (uint)slices.Length)
            {
                continue;
            }

            var (start, length) = slices[requirementIndex];

            if (start < 0)
            {
                continue;
            }

            if (TryResolveRuntimeTypeFromSnapshotScope(
                values.Slice(start, length),
                out runtimeTypeName,
                out runtimeType))
            {
                return true;
            }
        }

        runtimeTypeName = null!;
        runtimeType = null!;
        return false;
    }

    private bool TryResolveRuntimeTypeFromSnapshotScope(
        ReadOnlySequence<byte> scope,
        out string runtimeTypeName,
        out IOutputTypeDefinition runtimeType)
    {
        runtimeTypeName = null!;
        runtimeType = null!;

        if (!TryResolveProperty(scope, TypeNameFieldName, out var typeNameValue)
            || !TryReadString(typeNameValue, out var typeName))
        {
            return false;
        }

        runtimeTypeName = typeName;
        runtimeType = _schema.Types.GetType<IOutputTypeDefinition>(typeName);
        return true;
    }

    private static bool TryReadString(ReadOnlySequence<byte> value, out string result)
    {
        result = null!;
        var reader = new Utf8JsonReader(value);

        if (!reader.Read()
            || reader.TokenType is not JsonTokenType.String
            || reader.GetString() is not { } stringValue)
        {
            return false;
        }

        result = stringValue;
        return true;
    }

    private static bool TryResolveProperty(
        ReadOnlySequence<byte> scope,
        string propertyName,
        out ReadOnlySequence<byte> value)
    {
        value = default;
        var reader = new Utf8JsonReader(scope);

        if (!reader.Read() || reader.TokenType is not JsonTokenType.StartObject)
        {
            return false;
        }

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                return false;
            }

            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                return false;
            }

            var matches = reader.ValueTextEquals(propertyName);

            if (!reader.Read())
            {
                return false;
            }

            var start = reader.TokenStartIndex;
            reader.Skip();
            var length = reader.BytesConsumed - start;

            if (matches)
            {
                value = scope.Slice(start, length);
                return true;
            }
        }

        return false;
    }

    private static void WriteRawJsonValue(JsonWriter writer, ReadOnlySequence<byte> value)
    {
        if (value.IsSingleSegment)
        {
            writer.WriteRawValue(value.FirstSpan);
            return;
        }

        writer.WriteRawValue(value.ToArray());
    }

    private bool SaveRepresentationResult(
        CompositeResultElement resultData,
        SourceResultElement dataElement,
        ErrorTrie? errorTrie,
        ReadOnlySpan<EntityResultPath> resultPaths,
        ResultSelectionSet resultSelectionSet)
    {
        if (errorTrie is null)
        {
            var i = 0;

            foreach (var entity in dataElement.EnumerateArray())
            {
                ref readonly var resultPath = ref resultPaths[i++];

                if (!SaveSafeResult(
                        resultData,
                        resultPath.Path,
                        resultPath.AdditionalPaths.AsSpan(),
                        entity,
                        errorTrie: null,
                        resultSelectionSet))
                {
                    return false;
                }
            }

            return true;
        }

        var index = 0;

        foreach (var entity in dataElement.EnumerateArray())
        {
            ref readonly var resultPath = ref resultPaths[index];
            errorTrie.TryGetValue(index, out var entityErrorTrie);

            if (!SaveSafeResult(
                    resultData,
                    resultPath.Path,
                    resultPath.AdditionalPaths.AsSpan(),
                    entity,
                    entityErrorTrie,
                    resultSelectionSet))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    private bool SaveRepresentationSourceErrors(
        CompositeResultElement resultData,
        SourceSchemaErrors errors,
        ErrorTrie? errorTrie,
        ReadOnlySpan<EntityResultPath> resultPaths,
        ResultSelectionSet resultSelectionSet)
    {
        if (!errors.RootErrors.IsDefaultOrEmpty)
        {
            _errors ??= [];

            foreach (var rootError in errors.RootErrors)
            {
                _errors.Add(_errorHandler.Handle(rootError));
            }

            if (!CompleteRepresentationErrorTargets(
                    resultData,
                    resultPaths,
                    resultSelectionSet))
            {
                return false;
            }
        }

        return errorTrie is null
            || AddRepresentationErrors(resultData, errorTrie, resultPaths);
    }

    private bool CompleteRepresentationErrorTargets(
        CompositeResultElement resultData,
        ReadOnlySpan<EntityResultPath> resultPaths,
        ResultSelectionSet resultSelectionSet)
    {
        for (var i = 0; i < resultPaths.Length; i++)
        {
            ref readonly var resultPath = ref resultPaths[i];

            if (!Complete(resultPath.Path))
            {
                return false;
            }

            foreach (var additionalPath in resultPath.AdditionalPaths)
            {
                if (!Complete(additionalPath))
                {
                    return false;
                }
            }
        }

        return true;

        bool Complete(CompactPath targetPath)
        {
            if (resultData.IsInvalidated)
            {
                return false;
            }

            var target = targetPath.IsRoot ? resultData : GetStartObjectResult(targetPath);
            return target.IsNullOrInvalidated
                || _valueCompletion.CompleteErrorResult(target, resultSelectionSet);
        }
    }

    private bool AddRepresentationErrors(
        CompositeResultElement resultData,
        ErrorTrie errorTrie,
        ReadOnlySpan<EntityResultPath> resultPaths)
    {
        if (resultPaths.IsEmpty)
        {
            AddOriginalRepresentationErrors(errorTrie);
            return true;
        }

        if (errorTrie.Error is { } rootError)
        {
            for (var i = 0; i < resultPaths.Length; i++)
            {
                if (!AddRepresentationError(resultData, rootError, resultPaths[i], null, null))
                {
                    return false;
                }
            }
        }

        foreach (var (segment, childTrie) in errorTrie)
        {
            if (segment is int index && (uint)index < (uint)resultPaths.Length)
            {
                if (!AddRepresentationError(resultData, null, resultPaths[index], null, childTrie))
                {
                    return false;
                }
                continue;
            }

            if (segment is int)
            {
                AddOriginalRepresentationErrors(childTrie);
                continue;
            }

            for (var i = 0; i < resultPaths.Length; i++)
            {
                if (!AddRepresentationError(resultData, null, resultPaths[i], segment, childTrie))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void AddOriginalRepresentationErrors(ErrorTrie errorTrie)
    {
        if (errorTrie.Error is { } error)
        {
            _errors ??= [];
            _errors.Add(_errorHandler.Handle(error));
        }

        foreach (var childTrie in errorTrie.Values)
        {
            AddOriginalRepresentationErrors(childTrie);
        }
    }

    private bool AddRepresentationError(
        CompositeResultElement resultData,
        IError? error,
        EntityResultPath resultPath,
        object? segment,
        ErrorTrie? suffixTrie)
    {
        var path = resultPath.Path.ToPath(resultData.Operation);
        if (segment is not null)
        {
            path = AppendPathSegment(path, segment);
        }

        if (!AddRepresentationError(error, suffixTrie, path))
        {
            return false;
        }

        foreach (var additionalPath in resultPath.AdditionalPaths)
        {
            path = additionalPath.ToPath(resultData.Operation);
            if (segment is not null)
            {
                path = AppendPathSegment(path, segment);
            }

            if (!AddRepresentationError(error, suffixTrie, path))
            {
                return false;
            }
        }

        return true;
    }

    private bool AddRepresentationError(IError? error, ErrorTrie? errorTrie, Path path)
    {
        if (error is not null && !_valueCompletion.BuildErrorResult(path, error))
        {
            return false;
        }

        if (errorTrie is null)
        {
            return true;
        }

        if (errorTrie.Error is { } trieError
            && !_valueCompletion.BuildErrorResult(path, trieError))
        {
            return false;
        }

        foreach (var (segment, childTrie) in errorTrie)
        {
            if (!AddRepresentationError(null, childTrie, AppendPathSegment(path, segment)))
            {
                return false;
            }
        }

        return true;
    }

    private bool SaveRepresentationError(
        CompositeResultElement resultData,
        IError error,
        ReadOnlySpan<EntityResultPath> resultPaths,
        ResultSelectionSet resultSelectionSet)
    {
        if (resultPaths.IsEmpty)
        {
            _errors ??= [];
            _errors.Add(_errorHandler.Handle(error));
            return true;
        }

        for (var i = 0; i < resultPaths.Length; i++)
        {
            ref readonly var resultPath = ref resultPaths[i];

            if (!SaveRepresentationError(
                    resultData,
                    resultPath.Path,
                    resultPath.AdditionalPaths.AsSpan(),
                    error,
                    resultSelectionSet))
            {
                return false;
            }
        }

        return true;
    }

    private bool SaveRepresentationProtocolError(
        CompositeResultElement resultData,
        Exception exception,
        ReadOnlySpan<EntityResultPath> resultPaths,
        ResultSelectionSet resultSelectionSet)
    {
        var error = ErrorBuilder.New()
            .SetMessage(exception.Message)
            .SetException(exception)
            .Build();

        if (resultPaths.IsEmpty)
        {
            _errors ??= [];
            _errors.Add(_errorHandler.Handle(error));
            return true;
        }

        for (var i = 0; i < resultPaths.Length; i++)
        {
            ref readonly var resultPath = ref resultPaths[i];

            if (!SaveRepresentationError(
                    resultData,
                    resultPath.Path,
                    resultPath.AdditionalPaths.AsSpan(),
                    error,
                    resultSelectionSet))
            {
                return false;
            }
        }

        return true;
    }

    private bool SaveRepresentationError(
        CompositeResultElement resultData,
        CompactPath path,
        ReadOnlySpan<CompactPath> additionalPaths,
        IError error,
        ResultSelectionSet resultSelectionSet)
    {
        if (!AddError(path))
        {
            return false;
        }

        for (var i = 0; i < additionalPaths.Length; i++)
        {
            if (!AddError(additionalPaths[i]))
            {
                return false;
            }
        }

        return true;

        bool AddError(CompactPath targetPath)
        {
            if (resultData.IsInvalidated)
            {
                return false;
            }

            var target = targetPath.IsRoot ? resultData : GetStartObjectResult(targetPath);
            if (target.IsNullOrInvalidated)
            {
                return true;
            }

            if (_valueCompletion.BuildErrorResult(
                    target,
                    resultSelectionSet,
                    error,
                    target.CompactPath))
            {
                return true;
            }

            resultData.Invalidate();
            return false;
        }
    }

    private static Path AppendPathSegment(Path path, object segment)
        => segment switch
        {
            string name => path.Append(name),
            int index => path.Append(index),
            _ => throw new UnreachableException()
        };

    private void ReturnRepresentationPathSegments(RepresentationValue representation)
    {
        if (representation.ResultPaths.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var resultPath in representation.ResultPaths)
        {
            ReturnPathSegments(resultPath.Path, _seenPaths);

            foreach (var additionalPath in resultPath.AdditionalPaths)
            {
                ReturnPathSegments(additionalPath, _seenPaths);
            }
        }

        _seenPaths.Clear();
    }

    private enum RepresentationWriteResult
    {
        Skipped,
        Duplicate,
        Written
    }

    private ref struct RepresentationPathAccumulator
    {
        private readonly int _capacityHint;
        private CompactPath[]? _paths;
        private int[]? _slotIndices;
        private int _count;

        public RepresentationPathAccumulator(int capacityHint)
        {
            _capacityHint = capacityHint;
            _paths = null;
            _slotIndices = null;
            _count = 0;
        }

        public void Add(int slotIndex, CompactPath path)
        {
            if (_paths is null)
            {
                var capacity = Math.Max(16, _capacityHint);
                _paths = ArrayPool<CompactPath>.Shared.Rent(capacity);
                _slotIndices = ArrayPool<int>.Shared.Rent(capacity);
            }
            else if (_count == _paths.Length)
            {
                Grow();
            }

            _paths[_count] = path;
            _slotIndices![_count] = slotIndex;
            _count++;
        }

        public void AddRange(int slotIndex, ReadOnlySpan<CompactPath> paths)
        {
            foreach (var path in paths)
            {
                Add(slotIndex, path);
            }
        }

        public void ApplyTo(EntityResultPath[] resultPaths, int slotCount)
        {
            if (_count == 0)
            {
                return;
            }

            var counts = slotCount <= 256
                ? stackalloc int[slotCount]
                : new int[slotCount];

            for (var i = 0; i < _count; i++)
            {
                counts[_slotIndices![i]]++;
            }

            var offsets = slotCount <= 256
                ? stackalloc int[slotCount]
                : new int[slotCount];

            offsets[0] = 0;
            for (var i = 1; i < slotCount; i++)
            {
                offsets[i] = offsets[i - 1] + counts[i - 1];
            }

            var writePos = slotCount <= 256
                ? stackalloc int[slotCount]
                : new int[slotCount];
            offsets.CopyTo(writePos);

            var shared = new CompactPath[_count];

            for (var i = 0; i < _count; i++)
            {
                var idx = _slotIndices![i];
                shared[writePos[idx]++] = _paths![i];
            }

            for (var slot = 0; slot < slotCount; slot++)
            {
                if (counts[slot] == 0)
                {
                    continue;
                }

                resultPaths[slot] = resultPaths[slot] with
                {
                    AdditionalPaths = new CompactPathSegment(shared, offsets[slot], counts[slot])
                };
            }
        }

        private void Grow()
        {
            var newSize = _paths!.Length * 2;

            var newPaths = ArrayPool<CompactPath>.Shared.Rent(newSize);
            _paths.AsSpan(0, _count).CopyTo(newPaths);
            _paths.AsSpan(0, _count).Clear();
            ArrayPool<CompactPath>.Shared.Return(_paths);
            _paths = newPaths;

            var newIndices = ArrayPool<int>.Shared.Rent(newSize);
            _slotIndices.AsSpan(0, _count).CopyTo(newIndices);
            ArrayPool<int>.Shared.Return(_slotIndices!);
            _slotIndices = newIndices;
        }

        public void Dispose()
        {
            if (_paths is not null)
            {
                _paths.AsSpan(0, _count).Clear();
                ArrayPool<CompactPath>.Shared.Return(_paths);
                _paths = null;
            }

            if (_slotIndices is not null)
            {
                ArrayPool<int>.Shared.Return(_slotIndices);
                _slotIndices = null;
            }

            _count = 0;
        }
    }
}
