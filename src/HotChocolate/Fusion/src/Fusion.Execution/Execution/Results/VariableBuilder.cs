using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Produces deduplicated <see cref="VariableValues"/> sets for a subgraph fetch.
/// Instances are rented from a per-store pool so concurrent fetches can build their
/// variable values without contending on shared writer or dedup state.
/// </summary>
internal sealed class VariableBuilder : IDisposable
{
    private const int InitialCollectTargetCapacity = 64;
    private const int MaxCollectTargetRetainLength = 256;

    private static readonly ArrayPool<CompositeResultElement> s_pool = ArrayPool<CompositeResultElement>.Shared;

    private readonly ChunkedArrayWriter _writer = new();
    private readonly JsonWriter _jsonWriter;
    private readonly VariableDedupTable _dedupTable;

    private CompositeResultElement[] _collectTargetA = s_pool.Rent(InitialCollectTargetCapacity);
    private CompositeResultElement[] _collectTargetB = s_pool.Rent(InitialCollectTargetCapacity);
    private CompositeResultElement[] _collectTargetCombined = s_pool.Rent(InitialCollectTargetCapacity);

    private bool _disposed;

    public VariableBuilder()
    {
        _jsonWriter = new JsonWriter(_writer, new JsonWriterOptions { Indented = false });
        _dedupTable = new VariableDedupTable(_writer);
    }

    /// <summary>
    /// Builds a deduplicated set of <see cref="VariableValues"/> for every element reached
    /// by <paramref name="selectionSet"/> from <paramref name="root"/>.
    /// </summary>
    public ImmutableArray<VariableValues> Build(
        ISchemaDefinition schema,
        CompositeResultElement root,
        SelectionPath selectionSet,
        IReadOnlyList<ObjectFieldNode> forwardedVariables,
        ReadOnlySpan<OperationRequirement> requirements)
    {
        var elements = CollectTargetElements(root, selectionSet);

        if (elements.IsEmpty)
        {
            return [];
        }

        return BuildCore(schema, elements, forwardedVariables, requirements);
    }

    /// <summary>
    /// Builds a deduplicated set of <see cref="VariableValues"/> across all elements reached
    /// by any path in <paramref name="selectionSets"/>. Elements from different paths that
    /// produce identical variable values are merged via
    /// <see cref="VariableValues.AdditionalPaths"/>.
    /// </summary>
    public ImmutableArray<VariableValues> Build(
        ISchemaDefinition schema,
        CompositeResultElement root,
        ReadOnlySpan<SelectionPath> selectionSets,
        IReadOnlyList<ObjectFieldNode> forwardedVariables,
        ReadOnlySpan<OperationRequirement> requirements)
    {
        var combinedCount = 0;

        foreach (var selectionSet in selectionSets)
        {
            var elements = CollectTargetElements(root, selectionSet);

            if (!elements.IsEmpty)
            {
                EnsureCombinedCapacity(combinedCount + elements.Length, combinedCount);
                elements.CopyTo(_collectTargetCombined.AsSpan(combinedCount));
                combinedCount += elements.Length;
            }
        }

        if (combinedCount == 0)
        {
            return [];
        }

        return BuildCore(
            schema,
            _collectTargetCombined.AsSpan(0, combinedCount),
            forwardedVariables,
            requirements);
    }

    /// <summary>
    /// Builds a single <see cref="VariableValues"/> entry from the supplied forwarded
    /// variable fields. Used when a subgraph operation has no requirements and therefore
    /// no root element to collect from.
    /// </summary>
    public VariableValues Build(CompactPath path, IReadOnlyList<ObjectFieldNode> forwardedVariables)
    {
        _jsonWriter.Reset(_writer);
        var startPosition = _writer.Position;
        _jsonWriter.WriteStartObject();

        for (var i = 0; i < forwardedVariables.Count; i++)
        {
            var field = forwardedVariables[i];
            _jsonWriter.WritePropertyName(field.Name.Value);
            WriteValueNode(field.Value);
        }

        _jsonWriter.WriteEndObject();
        var length = _writer.Position - startPosition;

        // The returned JsonSegment references the builder's writer. The builder pool's
        // Return does not reset the writer, so these bytes remain valid until Clean().
        return new VariableValues(path, JsonSegment.Create(_writer, startPosition, length));
    }

    public void Clean()
    {
        _writer.Clean();
        _dedupTable.Clear();

        TrimOrClearBuffer(ref _collectTargetA);
        TrimOrClearBuffer(ref _collectTargetB);
        TrimOrClearBuffer(ref _collectTargetCombined);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        s_pool.Return(_collectTargetA, clearArray: true);
        s_pool.Return(_collectTargetB, clearArray: true);
        s_pool.Return(_collectTargetCombined, clearArray: true);
        _collectTargetA = [];
        _collectTargetB = [];
        _collectTargetCombined = [];

        _writer.Dispose();
        _dedupTable.Dispose();
    }

    // Reads from the result without synchronization: dependency tracking guarantees
    // the producer of every path read here has already completed. The only concurrent
    // mutation possible is null propagation from a sibling error, which is a single
    // atomic SetFlags write.
    private ReadOnlySpan<CompositeResultElement> CollectTargetElements(
        CompositeResultElement root,
        SelectionPath selectionSet)
    {
        var current = _collectTargetA;
        var currentCount = 0;
        var next = _collectTargetB;
        var nextCount = 0;

        current[currentCount++] = root;

        for (var i = 0; i < selectionSet.Length; i++)
        {
            var segment = selectionSet[i];

            if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
            {
                for (var j = 0; j < currentCount; j++)
                {
                    var element = current[j];
                    if (element.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var value)
                        && value.ValueKind is JsonValueKind.String
                        && value.TextEqualsHelper(segment.Name, isPropertyName: false))
                    {
                        AddToBuffer(ref next, ref nextCount, element);
                    }
                }
            }
            else if (segment.Kind is SelectionPathSegmentKind.Field)
            {
                for (var j = 0; j < currentCount; j++)
                {
                    var element = current[j];
                    if (!element.TryGetProperty(segment.Name, out var value))
                    {
                        continue;
                    }

                    var valueKind = value.ValueKind;

                    if (valueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    {
                        continue;
                    }

                    if (valueKind is JsonValueKind.Array)
                    {
                        AppendUnrolledLists(value, ref next, ref nextCount);
                        continue;
                    }

                    if (valueKind is JsonValueKind.Object)
                    {
                        AddToBuffer(ref next, ref nextCount, value);
                        continue;
                    }

                    // TODO : Better error
                    throw new NotSupportedException("Must be list or object.");
                }
            }

            (current, next) = (next, current);
            (currentCount, nextCount) = (nextCount, 0);

            if (currentCount == 0)
            {
                _collectTargetA = current;
                _collectTargetB = next;
                return [];
            }
        }

        _collectTargetA = current;
        _collectTargetB = next;
        return current.AsSpan(0, currentCount);
    }

    private ImmutableArray<VariableValues> BuildCore(
        ISchemaDefinition schema,
        ReadOnlySpan<CompositeResultElement> elements,
        IReadOnlyList<ObjectFieldNode> forwardedVariables,
        ReadOnlySpan<OperationRequirement> requirements)
    {
        _dedupTable.Initialize(elements.Length);

        if (forwardedVariables.Count == 0)
        {
            var fastPathResult = requirements.Length switch
            {
                1 => BuildSingleRequirement(schema, elements, requirements[0]),
                2 => BuildTwoRequirements(schema, elements, requirements[0], requirements[1]),
                3 => BuildThreeRequirements(schema, elements, requirements[0], requirements[1], requirements[2]),
                _ => default
            };

            if (!fastPathResult.IsDefault)
            {
                return fastPathResult;
            }
        }

        return BuildSlow(schema, elements, forwardedVariables, requirements);
    }

    private ImmutableArray<VariableValues> BuildSlow(
        ISchemaDefinition schema,
        ReadOnlySpan<CompositeResultElement> elements,
        IReadOnlyList<ObjectFieldNode> forwardedVariables,
        ReadOnlySpan<OperationRequirement> requirements)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            variableValueSets ??= new VariableValues[elements.Length];

            _jsonWriter.Reset(_writer);
            var startPosition = _writer.Position;
            _jsonWriter.WriteStartObject();

            for (var i = 0; i < forwardedVariables.Count; i++)
            {
                var field = forwardedVariables[i];
                _jsonWriter.WritePropertyName(field.Name.Value);
                WriteValueNode(field.Value);
            }

            var failed = false;

            foreach (var requirement in requirements)
            {
                _jsonWriter.WritePropertyName(requirement.Key);

                if (!ResultDataMapper.TryMap(result, requirement.Map, schema, _jsonWriter))
                {
                    failed = true;
                    break;
                }
            }

            if (failed)
            {
                _writer.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _dedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildSingleRequirement(
        ISchemaDefinition schema,
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement)
    {
        if (TryGetSimpleRequirementFieldName(requirement.Map, out var fieldName))
        {
            return BuildSingleRequirementFast(elements, requirement, fieldName);
        }

        return BuildSingleRequirementSlow(schema, elements, requirement);
    }

    private ImmutableArray<VariableValues> BuildSingleRequirementFast(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement,
        string fieldName)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;
        var isNonNullRequirement = requirement.Type.Kind is SyntaxKind.NonNullType;

        for (var i = 0; i < elements.Length; i++)
        {
            var result = elements[i];

            if (!result.TryGetProperty(fieldName, out var value))
            {
                continue;
            }

            var valueKind = value.ValueKind;

            if (valueKind is JsonValueKind.Undefined)
            {
                continue;
            }

            if (valueKind is JsonValueKind.Null && isNonNullRequirement)
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Length];

            _jsonWriter.Reset(_writer);
            var startPosition = _writer.Position;

            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement.Key);
            WriteCompositeResultValue(value);
            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _dedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildSingleRequirementSlow(
        ISchemaDefinition schema,
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            variableValueSets ??= new VariableValues[elements.Length];

            _jsonWriter.Reset(_writer);
            var startPosition = _writer.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement.Key);

            if (!ResultDataMapper.TryMap(result, requirement.Map, schema, _jsonWriter))
            {
                _writer.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _dedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildTwoRequirements(
        ISchemaDefinition schema,
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2)
    {
        if (TryGetSimpleRequirementFieldName(requirement1.Map, out var fieldName1)
            && TryGetSimpleRequirementFieldName(requirement2.Map, out var fieldName2))
        {
            return BuildTwoRequirementsFast(elements, requirement1, fieldName1, requirement2, fieldName2);
        }

        return BuildTwoRequirementsSlow(schema, elements, requirement1, requirement2);
    }

    private ImmutableArray<VariableValues> BuildTwoRequirementsFast(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        string fieldName1,
        OperationRequirement requirement2,
        string fieldName2)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || (value1.ValueKind is JsonValueKind.Null
                    && requirement1.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || (value2.ValueKind is JsonValueKind.Null
                    && requirement2.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Length];

            _jsonWriter.Reset(_writer);
            var startPosition = _writer.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement1.Key);
            WriteCompositeResultValue(value1);
            _jsonWriter.WritePropertyName(requirement2.Key);
            WriteCompositeResultValue(value2);
            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _dedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildTwoRequirementsSlow(
        ISchemaDefinition schema,
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            variableValueSets ??= new VariableValues[elements.Length];

            _jsonWriter.Reset(_writer);
            var startPosition = _writer.Position;
            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName(requirement1.Key);

            if (!ResultDataMapper.TryMap(result, requirement1.Map, schema, _jsonWriter))
            {
                _writer.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement2.Key);

            if (!ResultDataMapper.TryMap(result, requirement2.Map, schema, _jsonWriter))
            {
                _writer.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _dedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildThreeRequirements(
        ISchemaDefinition schema,
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3)
    {
        if (TryGetSimpleRequirementFieldName(requirement1.Map, out var fieldName1)
            && TryGetSimpleRequirementFieldName(requirement2.Map, out var fieldName2)
            && TryGetSimpleRequirementFieldName(requirement3.Map, out var fieldName3))
        {
            return BuildThreeRequirementsFast(
                elements,
                requirement1, fieldName1,
                requirement2, fieldName2,
                requirement3, fieldName3);
        }

        return BuildThreeRequirementsSlow(schema, elements, requirement1, requirement2, requirement3);
    }

    private ImmutableArray<VariableValues> BuildThreeRequirementsFast(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        string fieldName1,
        OperationRequirement requirement2,
        string fieldName2,
        OperationRequirement requirement3,
        string fieldName3)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || (value1.ValueKind is JsonValueKind.Null
                    && requirement1.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || (value2.ValueKind is JsonValueKind.Null
                    && requirement2.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName3, out var value3)
                || value3.ValueKind is JsonValueKind.Undefined
                || (value3.ValueKind is JsonValueKind.Null
                    && requirement3.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Length];

            _jsonWriter.Reset(_writer);
            var startPosition = _writer.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement1.Key);
            WriteCompositeResultValue(value1);
            _jsonWriter.WritePropertyName(requirement2.Key);
            WriteCompositeResultValue(value2);
            _jsonWriter.WritePropertyName(requirement3.Key);
            WriteCompositeResultValue(value3);
            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _dedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildThreeRequirementsSlow(
        ISchemaDefinition schema,
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            variableValueSets ??= new VariableValues[elements.Length];

            _jsonWriter.Reset(_writer);
            var startPosition = _writer.Position;
            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName(requirement1.Key);

            if (!ResultDataMapper.TryMap(result, requirement1.Map, schema, _jsonWriter))
            {
                _writer.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement2.Key);

            if (!ResultDataMapper.TryMap(result, requirement2.Map, schema, _jsonWriter))
            {
                _writer.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement3.Key);

            if (!ResultDataMapper.TryMap(result, requirement3.Map, schema, _jsonWriter))
            {
                _writer.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _dedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private static bool TryGetSimpleRequirementFieldName(
        IValueSelectionNode map,
        [NotNullWhen(true)] out string? fieldName)
    {
        if (map is PathNode
            {
                TypeName: null,
                PathSegment:
                {
                    TypeName: null,
                    PathSegment: null
                } pathSegment
            })
        {
            fieldName = pathSegment.FieldName.Value;
            return true;
        }

        fieldName = null;
        return false;
    }

    private VariableValues? TryCreateVariableValues(
        CompactPath path,
        int startPosition,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex)
    {
        var length = _writer.Position - startPosition;
        var hash = _writer.GetHashCode(startPosition, length);

        if (_dedupTable.TryGet(hash, startPosition, length, out var existingIndex))
        {
            additionalPaths.Add(existingIndex, path);
            _writer.ResetTo(startPosition);
            return null;
        }

        _dedupTable.Add(hash, nextIndex, startPosition, length);
        return new VariableValues(path, JsonSegment.Create(_writer, startPosition, length));
    }

    private void WriteValueNode(IValueNode value)
    {
        switch (value)
        {
            case NullValueNode:
                _jsonWriter.WriteNullValue();
                break;

            case StringValueNode sv:
                _jsonWriter.WriteStringValue(sv.Value);
                break;

            case IntValueNode iv:
                WriteRawAscii(iv.Value);
                break;

            case FloatValueNode fv:
                WriteRawAscii(fv.Value);
                break;

            case BooleanValueNode bv:
                _jsonWriter.WriteBooleanValue(bv.Value);
                break;

            case EnumValueNode ev:
                _jsonWriter.WriteStringValue(ev.Value);
                break;

            case ObjectValueNode ov:
                _jsonWriter.WriteStartObject();
                foreach (var field in ov.Fields)
                {
                    _jsonWriter.WritePropertyName(field.Name.Value);
                    WriteValueNode(field.Value);
                }
                _jsonWriter.WriteEndObject();
                break;

            case ListValueNode lv:
                _jsonWriter.WriteStartArray();
                foreach (var item in lv.Items)
                {
                    WriteValueNode(item);
                }
                _jsonWriter.WriteEndArray();
                break;

            default:
                _jsonWriter.WriteNullValue();
                break;
        }
    }

    private void WriteRawAscii(string value)
    {
        Span<byte> buffer = stackalloc byte[value.Length];
        System.Text.Encoding.UTF8.GetBytes(value.AsSpan(), buffer);
        _jsonWriter.WriteRawValue(buffer);
    }

    private void WriteCompositeResultValue(CompositeResultElement value)
        => value.WriteTo(_jsonWriter);

    private static ImmutableArray<VariableValues> FinalizeVariableValueSets(
        VariableValues[]? variableValueSets,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex)
    {
        if (variableValueSets is null || nextIndex == 0)
        {
            additionalPaths.Dispose();
            return [];
        }

        additionalPaths.ApplyTo(variableValueSets, nextIndex);
        additionalPaths.Dispose();

        if (variableValueSets.Length != nextIndex)
        {
            Array.Resize(ref variableValueSets, nextIndex);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(variableValueSets);
    }

    private void EnsureCombinedCapacity(int required, int count)
    {
        if (required > _collectTargetCombined.Length)
        {
            var newBuffer = s_pool.Rent(required);
            _collectTargetCombined.AsSpan(0, count).CopyTo(newBuffer);
            s_pool.Return(_collectTargetCombined, clearArray: true);
            _collectTargetCombined = newBuffer;
        }
    }

    private static void AppendUnrolledLists(
        CompositeResultElement list,
        ref CompositeResultElement[] destination,
        ref int destinationCount)
    {
        foreach (var element in list.EnumerateArray())
        {
            var elementValueKind = element.ValueKind;

            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            if (elementValueKind is JsonValueKind.Array)
            {
                AppendUnrolledLists(element, ref destination, ref destinationCount);
            }
            else
            {
                AddToBuffer(ref destination, ref destinationCount, element);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddToBuffer(
        ref CompositeResultElement[] buffer,
        ref int count,
        CompositeResultElement value)
    {
        if (count == buffer.Length)
        {
            GrowBuffer(ref buffer, count);
        }

        buffer[count++] = value;
    }

    private static void GrowBuffer(
        ref CompositeResultElement[] buffer,
        int count)
    {
        var newBuffer = s_pool.Rent(buffer.Length * 2);
        buffer.AsSpan(0, count).CopyTo(newBuffer);
        s_pool.Return(buffer, clearArray: true);
        buffer = newBuffer;
    }

    private static void TrimOrClearBuffer(ref CompositeResultElement[] buffer)
    {
        if (buffer.Length > MaxCollectTargetRetainLength)
        {
            s_pool.Return(buffer, clearArray: true);
            buffer = s_pool.Rent(InitialCollectTargetCapacity);
        }
        else
        {
            buffer.AsSpan().Clear();
        }
    }
}
