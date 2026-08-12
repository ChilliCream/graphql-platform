using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using FusionNameNode = HotChocolate.Fusion.Language.NameNode;
using IntValueNode = HotChocolate.Language.IntValueNode;
using FloatValueNode = HotChocolate.Language.FloatValueNode;
using StringValueNode = HotChocolate.Language.StringValueNode;
using BooleanValueNode = HotChocolate.Language.BooleanValueNode;
using NullValueNode = HotChocolate.Language.NullValueNode;
using EnumValueNode = HotChocolate.Language.EnumValueNode;
using ListValueNode = HotChocolate.Language.ListValueNode;
using ObjectValueNode = HotChocolate.Language.ObjectValueNode;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;
using IValueNode = HotChocolate.Language.IValueNode;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures the forwarded-variable serialization waste in the per-entry loops of
/// <c>FetchResultStore.BuildVariableValueSets</c> (FetchResultStore.cs lines
/// 1142-1147) and <c>FetchResultStore.BuildVariableValueSetsFromSnapshot</c>
/// (lines 1049-1054). When a fetch node forwards request variables, the requirement
/// fast paths are bypassed (guard at line 1094) and every entry re-serializes the
/// identical forwarded-variables AST through <c>WritePropertyName</c> plus
/// <c>WriteValueNode</c> (lines 1782-1833), a recursive type switch with string
/// escaping and number transcoding whose output bytes are constant across the loop.
///
/// The candidate serializes the forwarded variables once for the first non-empty
/// entry, copies the interior bytes between the object braces into a pooled buffer,
/// and splices those bytes into every further entry with one raw write. The public
/// <c>JsonWriter.WriteRawValue</c> (JsonWriter.cs lines 459-479) writes the same
/// bytes and leaves the same writer state as the internal <c>WriteRawValueStart</c>
/// plus <c>WriteRawValueEnd</c> pair (lines 487-523) that the product change would
/// use. As the accepted rider, requirement keys are transcoded to UTF-8 once per
/// call and written through the public byte-span <c>WritePropertyName</c> overload;
/// the product change would use the internal <c>WritePropertyNameUnescaped</c>
/// (JsonWriter.WriteValues.PropertyName.cs lines 230-244), which is not visible to
/// this project, so the measured rider keeps the escape scan and is slightly
/// conservative.
///
/// The workload drives the snapshot-path loop because its baseline is fully
/// reachable through the real internal <c>CreateVariableValueSetsFromSnapshot</c>
/// (FetchResultStore.cs lines 859-894) without seeding a composite result document,
/// and its per-entry serialization is identical in shape to the general element
/// loop. The product method is the reference baseline and includes the store's
/// pooling lifecycle (<c>Clean</c>, FetchResultStore.Pooling.cs lines 104-139); the
/// two replica methods share benchmark-owned writer machinery and an identical
/// writer-only clean, so the candidate's effect reads from the replica pair.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class ForwardedVariableBlitBenchmark
{
    /// <summary>
    /// BenchmarkDotNet 0.15.8 has no RuntimeMoniker for the net11.0 preview host and
    /// this project pins TargetFramework to net11.0, so out-of-process toolchains can
    /// neither validate nor build a child process here. The job therefore runs in
    /// process with the intended 3 warmup and 10 measurement iterations.
    /// </summary>
    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
            => AddJob(
                Job.Default
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithToolchain(InProcessEmitToolchain.Instance));
    }

    private const int MaxRetainedLength = 256;
    private const int RequirementKeyBufferSize = 64;
    private const string RequirementKey = "__fusion_1_id";

    // Mirrors FetchResultStore.cs line 37.
    private static readonly ArrayPool<VariableValues> s_variableValuePool =
        ArrayPool<VariableValues>.Shared;

    private static readonly OperationRequirement[] s_requirements =
        [Requirement(RequirementKey)];

    private static readonly HashSet<string> s_importedKeys =
        new([RequirementKey], StringComparer.Ordinal);

    private FetchResultStore _productStore = null!;
    private FetchResultStore _sourceStore = null!;
    private ChunkedArrayWriter _replicaWriter = null!;
    private JsonWriter _replicaJsonWriter = null!;
    private ReplicaVariableDedupTable _replicaDedup = null!;
    private ChunkedArrayWriter _blitWriter = null!;
    private JsonWriter _blitJsonWriter = null!;
    private ReplicaVariableDedupTable _blitDedup = null!;
    private ImmutableArray<VariableValues> _importedEntries;
    private IReadOnlyList<ObjectFieldNode> _requestVariables = null!;

    public long Consumed;

    /// <summary>
    /// The number of imported entries the merge iterates. 1 is the regression
    /// shape where the blit variant pays its one-time capture without ever
    /// splicing; 256 is the entity-batch shape where the win amortizes.
    /// </summary>
    [Params(1, 16, 256)]
    public int EntryCount { get; set; }

    /// <summary>
    /// Small is one forwarded int variable. Large is three forwarded variables
    /// including a nested object filter of roughly 25 AST nodes with strings that
    /// need escaping, the shape where per-entry re-serialization is most costly.
    /// </summary>
    [Params("Small", "Large")]
    public string VariableShape { get; set; } = "Small";

    [GlobalSetup]
    public void Setup()
    {
        _productStore = new FetchResultStore();
        _sourceStore = new FetchResultStore();

        // Mirrors the writer wiring of the FetchResultStore constructor
        // (FetchResultStore.cs lines 48-50 and 70-74).
        _replicaWriter = new ChunkedArrayWriter();
        _replicaJsonWriter = new JsonWriter(_replicaWriter, new JsonWriterOptions { Indented = false });
        _replicaDedup = new ReplicaVariableDedupTable(_replicaWriter);
        _blitWriter = new ChunkedArrayWriter();
        _blitJsonWriter = new JsonWriter(_blitWriter, new JsonWriterOptions { Indented = false });
        _blitDedup = new ReplicaVariableDedupTable(_blitWriter);

        _requestVariables = VariableShape == "Small"
            ? BuildSmallVariables()
            : BuildLargeVariables();

        var keyBytes = 0;

        foreach (var requirement in s_requirements)
        {
            keyBytes += Encoding.UTF8.GetByteCount(requirement.Key);
        }

        if (keyBytes > RequirementKeyBufferSize)
        {
            throw new InvalidOperationException(
                "The requirement keys do not fit the pre-encode buffer; "
                + "increase RequirementKeyBufferSize.");
        }

        _importedEntries = BuildImportedEntries(EntryCount);

        VerifyEquivalence();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _productStore.Dispose();
        _sourceStore.Dispose();
        _replicaDedup.Dispose();
        _replicaWriter.Dispose();
        _blitDedup.Dispose();
        _blitWriter.Dispose();
    }

    /// <summary>
    /// Current product behavior through the real internals: one snapshot merge via
    /// <c>CreateVariableValueSetsFromSnapshot</c> followed by the pooling
    /// <c>Clean</c> that production performs between requests. The store's Clean
    /// also resets collection buffers the replicas do not carry, so this method is
    /// the fidelity reference rather than the isolation partner of the blit.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long Merge_Product()
    {
        var result = _productStore.CreateVariableValueSetsFromSnapshot(
            _importedEntries,
            s_importedKeys,
            _requestVariables,
            s_requirements);

        var acc = Accumulate(result);
        _productStore.Clean(MaxRetainedLength, MaxRetainedLength);

        Consumed = acc;
        return acc;
    }

    /// <summary>
    /// Byte-faithful replica of the current per-entry loop over benchmark-owned
    /// writer machinery: every entry re-serializes the forwarded-variables AST.
    /// Its delta against <see cref="Merge_Product"/> bounds the replica's fidelity;
    /// its delta against <see cref="Merge_ReplicaPrefixBlit"/> isolates the
    /// candidate on identical machinery and lifecycle.
    /// </summary>
    [Benchmark]
    public long Merge_ReplicaPerEntrySerialize()
    {
        var result = BuildFromSnapshotCurrent(
            _replicaWriter,
            _replicaJsonWriter,
            _replicaDedup,
            _importedEntries,
            _requestVariables,
            s_requirements);

        var acc = Accumulate(result);
        _replicaWriter.Clean();

        Consumed = acc;
        return acc;
    }

    /// <summary>
    /// Candidate optimization: the forwarded variables serialize once for the first
    /// non-empty entry, the interior bytes are captured into a pooled buffer, and
    /// every further entry splices them with one raw write; requirement keys are
    /// UTF-8 encoded once per call.
    /// </summary>
    [Benchmark]
    public long Merge_ReplicaPrefixBlit()
    {
        var result = BuildFromSnapshotPrefixBlit(
            _blitWriter,
            _blitJsonWriter,
            _blitDedup,
            _importedEntries,
            _requestVariables,
            s_requirements);

        var acc = Accumulate(result);
        _blitWriter.Clean();

        Consumed = acc;
        return acc;
    }

    private static long Accumulate(ImmutableArray<VariableValues> values)
    {
        var acc = 0L;

        foreach (var entry in values)
        {
            acc += entry.Path.Length + entry.Values.Length + entry.AdditionalPaths.Length;
        }

        return acc;
    }

    /// <summary>
    /// Byte-faithful copy of <c>FetchResultStore.BuildVariableValueSetsFromSnapshot</c>
    /// (FetchResultStore.cs lines 1027-1084) over benchmark-owned writer machinery.
    /// The entry-point lock and argument guards of CreateVariableValueSetsFromSnapshot
    /// (lines 859-894) are constant per-call costs outside the measured loop and are
    /// omitted from both replicas.
    /// </summary>
    private static ImmutableArray<VariableValues> BuildFromSnapshotCurrent(
        ChunkedArrayWriter variableWriter,
        JsonWriter jsonWriter,
        ReplicaVariableDedupTable dedupTable,
        ImmutableArray<VariableValues> importedEntries,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        dedupTable.Initialize(importedEntries.Length);

        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var importedEntry in importedEntries)
        {
            if (importedEntry.IsEmpty)
            {
                continue;
            }

            jsonWriter.Reset(variableWriter);
            var startPosition = variableWriter.Position;
            jsonWriter.WriteStartObject();

            // The per-entry forwarded-variables re-serialization this candidate
            // removes (FetchResultStore.cs lines 1049-1054).
            for (var i = 0; i < requestVariables.Count; i++)
            {
                var field = requestVariables[i];
                jsonWriter.WritePropertyName(field.Name.Value);
                WriteValueNode(jsonWriter, field.Value);
            }

            if (!TryWriteRequestedRequirementValues(jsonWriter, importedEntry.Values, requiredData))
            {
                variableWriter.ResetTo(startPosition);
                continue;
            }

            jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(
                variableWriter,
                dedupTable,
                importedEntry.Path,
                startPosition,
                ref additionalPaths,
                nextIndex,
                out var dedupIndex);

            if (entry is null)
            {
                additionalPaths.AddRange(dedupIndex, importedEntry.AdditionalPaths.AsSpan());
                continue;
            }

            variableValueSets ??= s_variableValuePool.Rent(importedEntries.Length);
            variableValueSets[nextIndex] = entry.Value;
            additionalPaths.AddRange(nextIndex, importedEntry.AdditionalPaths.AsSpan());
            nextIndex++;
        }

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    /// <summary>
    /// The recommended design: identical to
    /// <see cref="BuildFromSnapshotCurrent"/> except that the forwarded variables
    /// serialize once, per-entry splicing is a raw byte copy, and requirement keys
    /// are pre-encoded once per call.
    /// </summary>
    private static ImmutableArray<VariableValues> BuildFromSnapshotPrefixBlit(
        ChunkedArrayWriter variableWriter,
        JsonWriter jsonWriter,
        ReplicaVariableDedupTable dedupTable,
        ImmutableArray<VariableValues> importedEntries,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        dedupTable.Initialize(importedEntries.Length);

        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        // Rider: requirement keys are GraphQL names (always ASCII), so they are
        // transcoded to UTF-8 once per call instead of per entry inside
        // TryWriteRequirementValue (FetchResultStore.cs line 1762).
        Span<byte> keyBytes = stackalloc byte[RequirementKeyBufferSize];
        Span<int> keyEnds = stackalloc int[requiredData.Length];
        var keyOffset = 0;

        for (var i = 0; i < requiredData.Length; i++)
        {
            keyOffset += Encoding.UTF8.GetBytes(requiredData[i].Key, keyBytes[keyOffset..]);
            keyEnds[i] = keyOffset;
        }

        byte[]? prefix = null;
        var prefixLength = -1;

        try
        {
            foreach (var importedEntry in importedEntries)
            {
                if (importedEntry.IsEmpty)
                {
                    continue;
                }

                jsonWriter.Reset(variableWriter);
                var startPosition = variableWriter.Position;
                jsonWriter.WriteStartObject();

                if (requestVariables.Count > 0)
                {
                    if (prefixLength < 0)
                    {
                        // First non-empty entry: serialize the forwarded variables
                        // exactly like the baseline and capture the interior bytes
                        // between the object braces before any requirement write can
                        // fail and rewind the writer. Writer positions are gap-free
                        // (ChunkedArrayWriter.cs lines 18-20), so the length is plain
                        // position arithmetic and CopyTo handles chunk crossings.
                        for (var i = 0; i < requestVariables.Count; i++)
                        {
                            var field = requestVariables[i];
                            jsonWriter.WritePropertyName(field.Name.Value);
                            WriteValueNode(jsonWriter, field.Value);
                        }

                        prefixLength = variableWriter.Position - startPosition - 1;
                        prefix = ArrayPool<byte>.Shared.Rent(prefixLength);
                        variableWriter.CopyTo(prefix.AsSpan(0, prefixLength), startPosition + 1, prefixLength);
                    }
                    else
                    {
                        // The public WriteRawValue (JsonWriter.cs lines 459-479)
                        // writes the same bytes and leaves the same writer state as
                        // the internal WriteRawValueStart plus WriteRawValueEnd pair
                        // (lines 487-523) for a contiguous span: no leading comma
                        // right after '{', and the list separator flag set so the
                        // next property name emits its comma.
                        jsonWriter.WriteRawValue(prefix!.AsSpan(0, prefixLength));
                    }
                }

                if (!TryWriteRequestedRequirementValuesPreEncoded(
                    jsonWriter,
                    importedEntry.Values,
                    requiredData,
                    keyBytes,
                    keyEnds))
                {
                    variableWriter.ResetTo(startPosition);
                    continue;
                }

                jsonWriter.WriteEndObject();

                var entry = TryCreateVariableValues(
                    variableWriter,
                    dedupTable,
                    importedEntry.Path,
                    startPosition,
                    ref additionalPaths,
                    nextIndex,
                    out var dedupIndex);

                if (entry is null)
                {
                    additionalPaths.AddRange(dedupIndex, importedEntry.AdditionalPaths.AsSpan());
                    continue;
                }

                variableValueSets ??= s_variableValuePool.Rent(importedEntries.Length);
                variableValueSets[nextIndex] = entry.Value;
                additionalPaths.AddRange(nextIndex, importedEntry.AdditionalPaths.AsSpan());
                nextIndex++;
            }

            return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
        }
        finally
        {
            if (prefix is not null)
            {
                ArrayPool<byte>.Shared.Return(prefix);
            }
        }
    }

    // Byte-faithful copy of FetchResultStore.TryCreateVariableValues
    // (FetchResultStore.cs lines 1679-1704).
    private static VariableValues? TryCreateVariableValues(
        ChunkedArrayWriter variableWriter,
        ReplicaVariableDedupTable dedupTable,
        CompactPath path,
        int startPosition,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex,
        out int dedupIndex)
    {
        var length = variableWriter.Position - startPosition;
        var hash = variableWriter.GetHashCode(startPosition, length);

        if (dedupTable.TryGet(hash, startPosition, length, out var existingIndex))
        {
            dedupIndex = existingIndex;
            additionalPaths.Add(existingIndex, path);
            variableWriter.ResetTo(startPosition);
            return null;
        }

        dedupIndex = nextIndex;
        dedupTable.Add(hash, nextIndex, startPosition, length);
        return new VariableValues(path, JsonSegment.Create(variableWriter, startPosition, length));
    }

    // Byte-faithful copy of FetchResultStore.TryWriteRequestedRequirementValues
    // (FetchResultStore.cs lines 1706-1726).
    private static bool TryWriteRequestedRequirementValues(
        JsonWriter jsonWriter,
        JsonSegment values,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        if (values.IsEmpty)
        {
            return false;
        }

        var sequence = values.AsSequence();

        foreach (var requirement in requiredData)
        {
            if (!TryWriteRequirementValue(jsonWriter, sequence, requirement.Key))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryWriteRequestedRequirementValuesPreEncoded(
        JsonWriter jsonWriter,
        JsonSegment values,
        ReadOnlySpan<OperationRequirement> requiredData,
        ReadOnlySpan<byte> keyBytes,
        ReadOnlySpan<int> keyEnds)
    {
        if (values.IsEmpty)
        {
            return false;
        }

        var sequence = values.AsSequence();
        var keyStart = 0;

        for (var i = 0; i < requiredData.Length; i++)
        {
            if (!TryWriteRequirementValuePreEncoded(
                jsonWriter,
                sequence,
                requiredData[i].Key,
                keyBytes[keyStart..keyEnds[i]]))
            {
                return false;
            }

            keyStart = keyEnds[i];
        }

        return true;
    }

    // Byte-faithful copy of FetchResultStore.TryWriteRequirementValue
    // (FetchResultStore.cs lines 1728-1769).
    private static bool TryWriteRequirementValue(
        JsonWriter jsonWriter,
        ReadOnlySequence<byte> values,
        string key)
    {
        var reader = new Utf8JsonReader(values);

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

            var matches = reader.ValueTextEquals(key);

            if (!reader.Read())
            {
                return false;
            }

            var start = reader.TokenStartIndex;
            reader.Skip();
            var length = reader.BytesConsumed - start;

            if (matches)
            {
                jsonWriter.WritePropertyName(key);
                WriteRawJsonValue(jsonWriter, values.Slice(start, length));
                return true;
            }
        }

        return false;
    }

    // Same scan as TryWriteRequirementValue; the only change (the accepted rider)
    // is that the property name write uses the pre-encoded UTF-8 key.
    private static bool TryWriteRequirementValuePreEncoded(
        JsonWriter jsonWriter,
        ReadOnlySequence<byte> values,
        string key,
        ReadOnlySpan<byte> utf8Key)
    {
        var reader = new Utf8JsonReader(values);

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

            var matches = reader.ValueTextEquals(key);

            if (!reader.Read())
            {
                return false;
            }

            var start = reader.TokenStartIndex;
            reader.Skip();
            var length = reader.BytesConsumed - start;

            if (matches)
            {
                jsonWriter.WritePropertyName(utf8Key);
                WriteRawJsonValue(jsonWriter, values.Slice(start, length));
                return true;
            }
        }

        return false;
    }

    // Byte-faithful copy of FetchResultStore.WriteRawJsonValue
    // (FetchResultStore.cs lines 1771-1780).
    private static void WriteRawJsonValue(JsonWriter jsonWriter, ReadOnlySequence<byte> value)
    {
        if (value.IsSingleSegment)
        {
            jsonWriter.WriteRawValue(value.FirstSpan);
            return;
        }

        jsonWriter.WriteRawValue(value.ToArray());
    }

    // Byte-faithful copy of FetchResultStore.WriteValueNode
    // (FetchResultStore.cs lines 1782-1833).
    private static void WriteValueNode(JsonWriter jsonWriter, IValueNode value)
    {
        switch (value)
        {
            case NullValueNode:
                jsonWriter.WriteNullValue();
                break;

            case StringValueNode sv:
                jsonWriter.WriteStringValue(sv.Value);
                break;

            case IntValueNode iv:
                WriteRawAscii(jsonWriter, iv.Value);
                break;

            case FloatValueNode fv:
                WriteRawAscii(jsonWriter, fv.Value);
                break;

            case BooleanValueNode bv:
                jsonWriter.WriteBooleanValue(bv.Value);
                break;

            case EnumValueNode ev:
                jsonWriter.WriteStringValue(ev.Value);
                break;

            case ObjectValueNode ov:
                jsonWriter.WriteStartObject();
                foreach (var field in ov.Fields)
                {
                    jsonWriter.WritePropertyName(field.Name.Value);
                    WriteValueNode(jsonWriter, field.Value);
                }
                jsonWriter.WriteEndObject();
                break;

            case ListValueNode lv:
                jsonWriter.WriteStartArray();
                foreach (var item in lv.Items)
                {
                    WriteValueNode(jsonWriter, item);
                }
                jsonWriter.WriteEndArray();
                break;

            default:
                jsonWriter.WriteNullValue();
                break;
        }
    }

    // Byte-faithful copy of FetchResultStore.WriteRawAscii
    // (FetchResultStore.cs lines 1835-1840).
    private static void WriteRawAscii(JsonWriter jsonWriter, string value)
    {
        Span<byte> buffer = stackalloc byte[value.Length];
        Encoding.UTF8.GetBytes(value.AsSpan(), buffer);
        jsonWriter.WriteRawValue(buffer);
    }

    // Byte-faithful copy of FetchResultStore.FinalizeVariableValueSets
    // (FetchResultStore.cs lines 2545-2570).
    private static ImmutableArray<VariableValues> FinalizeVariableValueSets(
        VariableValues[]? variableValueSets,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex)
    {
        if (variableValueSets is null || nextIndex == 0)
        {
            if (variableValueSets is not null)
            {
                s_variableValuePool.Return(variableValueSets, clearArray: true);
            }

            additionalPaths.Dispose();
            return [];
        }

        additionalPaths.ApplyTo(variableValueSets, nextIndex);
        additionalPaths.Dispose();

        var span = variableValueSets.AsSpan(0, nextIndex);
        var result = span.ToArray();
        span.Clear();
        s_variableValuePool.Return(variableValueSets);

        return ImmutableCollectionsMarshal.AsImmutableArray(result);
    }

    private static IReadOnlyList<ObjectFieldNode> BuildSmallVariables()
        => [new ObjectFieldNode("limit", new IntValueNode(10))];

    private static IReadOnlyList<ObjectFieldNode> BuildLargeVariables()
        =>
        [
            new ObjectFieldNode("first", new IntValueNode(25)),
            new ObjectFieldNode("locale", new StringValueNode("en-US")),
            new ObjectFieldNode(
                "filter",
                new ObjectValueNode(
                    new ObjectFieldNode("category", new EnumValueNode("ELECTRONICS")),
                    new ObjectFieldNode("minPrice", new IntValueNode(10)),
                    new ObjectFieldNode("maxPrice", new FloatValueNode(999.99)),
                    new ObjectFieldNode(
                        "tags",
                        new ListValueNode(
                            new StringValueNode("sale"),
                            new StringValueNode("new"),
                            new StringValueNode("featured"))),
                    new ObjectFieldNode("inStock", new BooleanValueNode(true)),
                    new ObjectFieldNode("cursor", NullValueNode.Default),
                    new ObjectFieldNode(
                        "nested",
                        new ObjectValueNode(
                            new ObjectFieldNode("brand", new StringValueNode("Acme \"Pro\" line")),
                            new ObjectFieldNode("rating", new IntValueNode(4))))))
        ];

    private ImmutableArray<VariableValues> BuildImportedEntries(int count)
    {
        var builder = ImmutableArray.CreateBuilder<VariableValues>(count);

        for (var i = 0; i < count; i++)
        {
            builder.Add(
                _sourceStore.CreateVariableValueSets(
                    Path(i),
                    [new ObjectFieldNode(RequirementKey, new StringValueNode($"id-{i}"))]));
        }

        return builder.MoveToImmutable();
    }

    /// <summary>
    /// A verification-only snapshot exercising every loop edge: an empty entry
    /// (skipped), a first non-empty entry whose values lack the requirement key
    /// (capture happens, then the requirement write fails and the writer rewinds),
    /// a duplicate pair (dedup hit accumulating an additional path), and a unique
    /// tail entry.
    /// </summary>
    private ImmutableArray<VariableValues> BuildEdgeCaseSnapshot()
        =>
        [
            default,
            _sourceStore.CreateVariableValueSets(
                Path(1000),
                [new ObjectFieldNode("unrelated", new StringValueNode("x"))]),
            _sourceStore.CreateVariableValueSets(
                Path(0),
                [new ObjectFieldNode(RequirementKey, new StringValueNode("dup"))]),
            _sourceStore.CreateVariableValueSets(
                Path(1),
                [new ObjectFieldNode(RequirementKey, new StringValueNode("dup"))]),
            _sourceStore.CreateVariableValueSets(
                Path(2),
                [new ObjectFieldNode(RequirementKey, new StringValueNode("unique"))])
        ];

    private void VerifyEquivalence()
    {
        VerifySnapshotEquivalence(_importedEntries, "measured", expectedEntryCount: EntryCount);
        VerifySnapshotEquivalence(BuildEdgeCaseSnapshot(), "edge", expectedEntryCount: 2);
    }

    private void VerifySnapshotEquivalence(
        ImmutableArray<VariableValues> snapshot,
        string workload,
        int expectedEntryCount)
    {
        var product = _productStore.CreateVariableValueSetsFromSnapshot(
            snapshot, s_importedKeys, _requestVariables, s_requirements);
        var productEntries = ExtractEntries(product);
        _productStore.Clean(MaxRetainedLength, MaxRetainedLength);

        var replica = BuildFromSnapshotCurrent(
            _replicaWriter, _replicaJsonWriter, _replicaDedup,
            snapshot, _requestVariables, s_requirements);
        var replicaEntries = ExtractEntries(replica);
        _replicaWriter.Clean();

        var blit = BuildFromSnapshotPrefixBlit(
            _blitWriter, _blitJsonWriter, _blitDedup,
            snapshot, _requestVariables, s_requirements);
        var blitEntries = ExtractEntries(blit);
        _blitWriter.Clean();

        if (productEntries.Count != expectedEntryCount)
        {
            throw new InvalidOperationException(
                $"The {workload} workload produced {productEntries.Count} entries instead of "
                + $"{expectedEntryCount}; the benchmark would measure an unintended path.");
        }

        CompareEntries(productEntries, replicaEntries, workload, "per-entry replica");
        CompareEntries(productEntries, blitEntries, workload, "prefix-blit replica");
    }

    private static List<ExtractedEntry> ExtractEntries(ImmutableArray<VariableValues> entries)
    {
        var extracted = new List<ExtractedEntry>(entries.Length);

        foreach (var entry in entries)
        {
            var additionalPaths = new int[entry.AdditionalPaths.Length][];
            var index = 0;

            foreach (var path in entry.AdditionalPaths)
            {
                additionalPaths[index++] = path.Segments.ToArray();
            }

            extracted.Add(
                new ExtractedEntry(
                    entry.Path.Segments.ToArray(),
                    entry.Values.AsSequence().ToArray(),
                    additionalPaths));
        }

        return extracted;
    }

    private static void CompareEntries(
        List<ExtractedEntry> expected,
        List<ExtractedEntry> actual,
        string workload,
        string variant)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException(
                $"{workload}/{variant}: expected {expected.Count} entries but got {actual.Count}.");
        }

        for (var i = 0; i < expected.Count; i++)
        {
            var e = expected[i];
            var a = actual[i];

            if (!e.Path.AsSpan().SequenceEqual(a.Path))
            {
                throw new InvalidOperationException(
                    $"{workload}/{variant}: entry {i} resolved a different path.");
            }

            if (!e.Values.AsSpan().SequenceEqual(a.Values))
            {
                throw new InvalidOperationException(
                    $"{workload}/{variant}: entry {i} produced different variable bytes. Expected "
                    + $"'{Encoding.UTF8.GetString(e.Values)}' but got '{Encoding.UTF8.GetString(a.Values)}'.");
            }

            if (e.AdditionalPaths.Length != a.AdditionalPaths.Length)
            {
                throw new InvalidOperationException(
                    $"{workload}/{variant}: entry {i} accumulated {a.AdditionalPaths.Length} "
                    + $"additional paths instead of {e.AdditionalPaths.Length}.");
            }

            for (var j = 0; j < e.AdditionalPaths.Length; j++)
            {
                if (!e.AdditionalPaths[j].AsSpan().SequenceEqual(a.AdditionalPaths[j]))
                {
                    throw new InvalidOperationException(
                        $"{workload}/{variant}: entry {i} additional path {j} differs.");
                }
            }
        }
    }

    private static OperationRequirement Requirement(string key)
        => new(
            key,
            new NamedTypeNode("String"),
            SelectionPath.Root,
            new PathNode(new PathSegmentNode(new FusionNameNode(key))),
            null);

    private static CompactPath Path(params int[] segments)
    {
        var buffer = new int[segments.Length + 1];
        buffer[0] = segments.Length;
        segments.CopyTo(buffer.AsSpan(1));
        return new CompactPath(buffer);
    }

    private sealed record ExtractedEntry(int[] Path, byte[] Values, int[][] AdditionalPaths);

    /// <summary>
    /// Byte-faithful copy of the private nested FetchResultStore.VariableDedupTable
    /// (FetchResultStore.cs lines 2572-2793), which is not reachable through
    /// InternalsVisibleTo. Both replica variants dedup through this copy so the
    /// hashing and probing costs stay identical between them.
    /// </summary>
    private sealed class ReplicaVariableDedupTable(ChunkedArrayWriter writer) : IDisposable
    {
        private const int DefaultBucketSize = 4;
        private const int DefaultBucketCount = 16;
        private const int TrackedSlotCapacity = 16;

        private readonly ChunkedArrayWriter _writer = writer;
        private Entry[] _table = RentClearedTable(DefaultBucketCount * DefaultBucketSize);
        private int[] _writtenSlots = [];
        private int _bucketCount = DefaultBucketCount;
        private readonly int _bucketSize = DefaultBucketSize;
        private int _writtenCount;
        private bool _clearFullTable;

        public void Initialize(int capacity)
        {
            var bucketCount = NextPowerOfTwo(Math.Max(capacity, DefaultBucketCount));
            var totalSize = bucketCount * _bucketSize;

            ClearPreviousEntries();

            if (_table.Length < totalSize)
            {
                Resize(totalSize);
            }

            _bucketCount = bucketCount;
        }

        public bool TryGet(
            int hash,
            int location,
            int length,
            out int existingIndex)
        {
            var bucket = hash & 0x7FFFFFFF & (_bucketCount - 1);
            var start = bucket * _bucketSize;
            var end = start + _bucketSize;

            for (var s = start; s < end; s++)
            {
                ref var entry = ref _table[s];

                if (entry.Index == 0)
                {
                    existingIndex = -1;
                    return false;
                }

                if (entry.Hash == hash
                    && entry.Length == length
                    && _writer.SequenceEqual(entry.Location, location, length))
                {
                    existingIndex = entry.Index - 1;
                    return true;
                }
            }

            existingIndex = -1;
            return false;
        }

        public void Add(int hash, int index, int location, int length)
        {
            var bucket = hash & 0x7FFFFFFF & (_bucketCount - 1);
            var start = bucket * _bucketSize;
            var end = start + _bucketSize;

            for (var s = start; s < end; s++)
            {
                ref var entry = ref _table[s];

                if (entry.Index == 0)
                {
                    RecordWrittenSlot(s);

                    entry.Hash = hash;
                    entry.Index = index + 1;
                    entry.Location = location;
                    entry.Length = length;
                    return;
                }
            }

            Grow();
            Add(hash, index, location, length);
        }

        public void Dispose()
        {
            if (_table.Length > 0)
            {
                ArrayPool<Entry>.Shared.Return(_table);
                _table = [];
            }

            if (_writtenSlots.Length > 0)
            {
                ArrayPool<int>.Shared.Return(_writtenSlots);
                _writtenSlots = [];
            }

            ResetClearState();
        }

        private void Grow()
        {
            var oldTable = _table;
            var oldTotal = _bucketCount * _bucketSize;
            var newBucketCount = _bucketCount * 2;
            var newTotal = newBucketCount * _bucketSize;
            var newTable = RentClearedTable(newTotal);

            _bucketCount = newBucketCount;
            _table = newTable;
            _clearFullTable = true;
            _writtenCount = 0;

            try
            {
                for (var i = 0; i < oldTotal; i++)
                {
                    var entry = oldTable[i];

                    if (entry.Index != 0)
                    {
                        Add(entry.Hash, entry.Index - 1, entry.Location, entry.Length);
                    }
                }
            }
            finally
            {
                ArrayPool<Entry>.Shared.Return(oldTable);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int totalSize)
        {
            var newTable = RentClearedTable(totalSize);
            var oldTable = _table;
            _table = newTable;
            ArrayPool<Entry>.Shared.Return(oldTable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordWrittenSlot(int slot)
        {
            if (_clearFullTable)
            {
                return;
            }

            if (_writtenCount == TrackedSlotCapacity)
            {
                _clearFullTable = true;
                _writtenCount = 0;
                return;
            }

            if (_writtenSlots.Length == 0)
            {
                RentWrittenSlots();
            }

            _writtenSlots[_writtenCount++] = slot;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RentWrittenSlots()
            => _writtenSlots = ArrayPool<int>.Shared.Rent(TrackedSlotCapacity);

        private void ClearPreviousEntries()
        {
            if (_clearFullTable)
            {
                _table.AsSpan(0, _bucketCount * _bucketSize).Clear();
            }
            else
            {
                for (var i = 0; i < _writtenCount; i++)
                {
                    _table[_writtenSlots[i]] = default;
                }
            }

            ResetClearState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetClearState()
        {
            _writtenCount = 0;
            _clearFullTable = false;
        }

        private static Entry[] RentClearedTable(int minimumLength)
        {
            var table = ArrayPool<Entry>.Shared.Rent(minimumLength);
            table.AsSpan().Clear();
            return table;
        }

        private static int NextPowerOfTwo(int n)
        {
            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            return n + 1;
        }

        private struct Entry
        {
            public int Hash;
            public int Index;    // 1-based (0 = empty)
            public int Location;
            public int Length;
        }
    }
}
