using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;
using DbRow = HotChocolate.Fusion.Text.Json.CompositeResultDocument.DbRow;
using ElementFlags = HotChocolate.Fusion.Text.Json.CompositeResultDocument.ElementFlags;
using OperationReferenceType = HotChocolate.Fusion.Text.Json.CompositeResultDocument.OperationReferenceType;
using FusionOperation = HotChocolate.Fusion.Execution.Nodes.Operation;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Compares two strategies for materializing the MetaDb rows that
/// <c>CompositeResultDocument.CreateObject</c> writes for one object instance
/// (CompositeResultDocument.cs lines 427-461):
///
///   - AppendLoop (baseline): a byte-faithful local copy of the current per-row
///     append sequence: AppendStartObject, then per selection either
///     AppendEmptyPropertyWithNullValue or the synthesized __typename pair
///     (AppendEmptyProperty + Append(String)), then AppendEndObject, each with
///     its own capacity checks, plus the per-selection introspection check and
///     GetPropertyFlags recompute against the real compiled Selection objects.
///
///   - TemplateBlockCopy (candidate): a (2N + 2)-row template pre-built once per
///     selection set, block-copied per instance with one whole-block capacity
///     check, then only the parent-pointer ints are stamped into the copied rows.
///
/// Both variants write <see cref="InstanceCount"/> instances of the same
/// 10-field Product selection set (one synthesized __typename among them) into a
/// pre-allocated 128 KB chunk, simulating a steady-state chunk of the real
/// MetaDb. GlobalSetup proves the baseline copy byte-identical to the real
/// CreateObject on a real CompositeResultDocument and proves both variants
/// produce byte-identical chunks.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class CompositeObjectCreateTemplateBenchmark : FusionBenchmarkBase
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

    private const string OperationId = "123456789101112";
    private const int InstanceCount = 100;
    private const int FieldCount = 10;

    // A steady-state MetaDb chunk (ChunkSize.Size128K => 1 << (10 + 7) bytes)
    // holds 6553 rows; 100 instances x 22 rows = 2200 rows fit comfortably.
    private const int ChunkBytes = 128 * 1024;

    // The document under test is created with neither @include/@skip variables
    // nor @defer, matching CompositeResultDocument._includeFlags/_deferFlags = 0.
    private const ulong IncludeFlags = 0;
    private const ulong DeferFlags = 0;

#pragma warning disable IDE0370 // Remove unnecessary suppression
    private FusionOperation _operation = null!;
    private SelectionSet _selectionSet = null!;
    private byte[] _baselineChunk = null!;
    private byte[] _templateChunk = null!;
    private byte[] _template = null!;
    private int[] _parents = null!;
#pragma warning restore IDE0370 // Remove unnecessary suppression
    private int _rowsPerInstance;
    private MetaDbClone _baselineDb;
    private MetaDbClone _templateDb;

    [GlobalSetup]
    public void Setup()
    {
        var schema = CreateFusionSchema();
        var documentRewriter = new DocumentRewriter(schema);
        var operationDefinition = documentRewriter
            .RewriteDocument(
                Utf8GraphQLParser.Parse(
                    """
                    {
                      productById(id: "1") {
                        __typename
                        id
                        name
                        description
                        price
                        dimension { height width }
                        estimatedDelivery
                        secondDescription: description
                        secondPrice: price
                        secondName: name
                      }
                    }
                    """))
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var fieldMapPool = new DefaultObjectPool<
            OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<
                    OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operationCompiler = new OperationCompiler(schema, fieldMapPool);
        _operation = operationCompiler.Compile(
            OperationId,
            OperationId,
            operationDefinition);

        var productSelection = _operation.RootSelectionSet.Selections[0];
        _selectionSet = _operation.GetSelectionSet(productSelection);

        var selections = _selectionSet.Selections;

        if (selections.Length != FieldCount)
        {
            throw new InvalidOperationException(
                $"Expected the Product selection set to contain {FieldCount} selections "
                + $"but found {selections.Length}.");
        }

        var typeNameCount = 0;

        foreach (var selection in selections)
        {
            if (selection.Field.IsIntrospectionField
                && selection.Field.Name == IntrospectionFieldNames.TypeName)
            {
                typeNameCount++;
            }
        }

        if (typeNameCount != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one __typename selection but found {typeNameCount}.");
        }

        _rowsPerInstance = (selections.Length * 2) + 2;

        _baselineChunk = new byte[ChunkBytes];
        _templateChunk = new byte[ChunkBytes];
        _baselineDb = MetaDbClone.Create(_baselineChunk);
        _templateDb = MetaDbClone.Create(_templateChunk);

        // Deterministic 29-bit parent cursor values, one per instance, mirroring
        // the distinct array-element value slots a list merge would stamp.
        var rng = new Random(42);
        _parents = new int[InstanceCount];

        for (var i = 0; i < InstanceCount; i++)
        {
            _parents[i] = rng.Next(0, 0x1FFFFFFF);
        }

        // Pre-build the row-block template once: the append loop rendered at
        // start cursor 0 with parent 0. Every byte except the parent ints is
        // invariant per (document, selectionSet); the parent ints are stamped
        // per instance by AppendTemplateBlock (the EndObject parent stays 0).
        _template = new byte[_rowsPerInstance * DbRow.Size];
        var templateDb = MetaDbClone.Create(_template);
        CreateObjectWithAppendLoop(ref templateDb, parentValue: 0);

        VerifyAppendLoopMatchesRealCreateObject();
        VerifyVariantsProduceIdenticalRows();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = InstanceCount)]
    public void CreateObject_AppendLoop()
    {
        _baselineDb.Reset(0);
        var parents = _parents;

        for (var i = 0; i < InstanceCount; i++)
        {
            CreateObjectWithAppendLoop(ref _baselineDb, parents[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = InstanceCount)]
    public void CreateObject_TemplateBlockCopy()
    {
        _templateDb.Reset(0);
        var template = _template;
        var parents = _parents;

        for (var i = 0; i < InstanceCount; i++)
        {
            _templateDb.AppendTemplateBlock(template, parents[i]);
        }
    }

    /// <summary>
    /// Byte-faithful local copy of CompositeResultDocument.CreateObject
    /// (CompositeResultDocument.cs lines 427-461). The CompositeObjectContext
    /// and CompositeResultElement constructions at lines 454-460 are plain
    /// struct initializations and are not replicated.
    /// </summary>
    private int CreateObjectWithAppendLoop(ref MetaDbClone db, int parentValue)
    {
        var selectionSet = _selectionSet;
        var selections = selectionSet.Selections;

        // WriteStartObject (CompositeResultDocument.cs lines 568-570).
        var startObjectCursor = db.AppendStartObject(
            parentValue,
            selectionSet.Id,
            selections.Length,
            ElementFlags.None);

        // Per-selection loop (CompositeResultDocument.cs lines 435-450).
        foreach (var selection in selections)
        {
            if (selection.Field.IsIntrospectionField
                && selection.Field.Name == IntrospectionFieldNames.TypeName)
            {
                // WriteTypeNameProperty (CompositeResultDocument.cs lines 589-600).
                var propertyCursor = db.AppendEmptyProperty(
                    startObjectCursor,
                    selection.Id,
                    GetPropertyFlags(selection));

                db.Append(
                    ElementTokenType.String,
                    location: selectionSet.Id,
                    parentRow: propertyCursor);
            }
            else
            {
                // WriteEmptyProperty (CompositeResultDocument.cs lines 579-584).
                db.AppendEmptyPropertyWithNullValue(
                    startObjectCursor,
                    selection.Id,
                    GetPropertyFlags(selection));
            }
        }

        db.AppendEndObject();

        return startObjectCursor;
    }

    /// <summary>
    /// Byte-faithful local copy of CompositeResultDocument.GetPropertyFlags
    /// (CompositeResultDocument.cs lines 602-628) with the document's
    /// includeFlags/deferFlags fixed to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ElementFlags GetPropertyFlags(Selection selection)
    {
        var flags = ElementFlags.None;

        if (selection.IsInternal)
        {
            flags = ElementFlags.IsInternal;
        }

        if (!selection.IsIncluded(IncludeFlags) || selection.IsDeferred(DeferFlags))
        {
            flags |= ElementFlags.IsExcluded;
        }

        if (selection.Type.Kind is not TypeKind.NonNull)
        {
            flags |= ElementFlags.IsNullable;
        }

        if (selection.IsEnumValue)
        {
            flags |= ElementFlags.IsEnumValue;
        }

        return flags;
    }

    /// <summary>
    /// Proves the local append-loop copy is byte-identical to the real
    /// CreateObject: builds a real CompositeResultDocument from the compiled
    /// operation, creates one object for the same selection set, and compares
    /// all of its MetaDb rows against the local writer's output for the same
    /// parent and start cursor values.
    /// </summary>
    private void VerifyAppendLoopMatchesRealCreateObject()
    {
        using var arena = new MemoryArena();
        using var document = new CompositeResultDocument(arena, _operation, includeFlags: 0);

        // Row 2 is the value slot of the root productById property, which is
        // exactly where a real merge would hang the created object.
        var parentCursor = document.Data.Cursor.AddRows(2);
        var startCursor = document._metaDb.NextCursor;
        var element = document.CreateObject(parentCursor, _selectionSet);

        if (element.Cursor != startCursor)
        {
            throw new InvalidOperationException(
                "The created object did not start at the expected cursor.");
        }

        if (startCursor.Chunk != 0)
        {
            throw new InvalidOperationException(
                "Expected the created object to be written into chunk 0.");
        }

        var expected = new byte[_rowsPerInstance * DbRow.Size];

        for (var i = 0; i < _rowsPerInstance; i++)
        {
            var row = document._metaDb.Get(startCursor.AddRows(i));
            MemoryMarshal.Write(expected.AsSpan(i * DbRow.Size, DbRow.Size), in row);
        }

        // Replay the local writer at the same start cursor value with the same
        // parent value; within chunk 0 the cursor value equals the row index.
        var chunk = new byte[1024];
        var db = MetaDbClone.Create(chunk);
        db.Reset(startCursor.Value);
        CreateObjectWithAppendLoop(ref db, parentCursor.Value);

        var actual = chunk.AsSpan(
            startCursor.Value * DbRow.Size,
            _rowsPerInstance * DbRow.Size);

        if (!actual.SequenceEqual(expected))
        {
            for (var i = 0; i < actual.Length; i++)
            {
                if (actual[i] != expected[i])
                {
                    throw new InvalidOperationException(
                        "The local append-loop copy diverged from the real CreateObject "
                        + $"rows at byte {i} (row {i / DbRow.Size}, offset {i % DbRow.Size}).");
                }
            }
        }
    }

    /// <summary>
    /// Runs both benchmark bodies once and compares the full written regions of
    /// both chunks byte for byte.
    /// </summary>
    private void VerifyVariantsProduceIdenticalRows()
    {
        CreateObject_AppendLoop();
        CreateObject_TemplateBlockCopy();

        var length = InstanceCount * _rowsPerInstance * DbRow.Size;
        var baseline = _baselineChunk.AsSpan(0, length);
        var template = _templateChunk.AsSpan(0, length);

        if (!baseline.SequenceEqual(template))
        {
            for (var i = 0; i < length; i++)
            {
                if (baseline[i] != template[i])
                {
                    throw new InvalidOperationException(
                        "Baseline and template variants produced different bytes at "
                        + $"offset {i} (row {i / DbRow.Size}, offset {i % DbRow.Size}).");
                }
            }
        }
    }

    /// <summary>
    /// Single-chunk stand-in for CompositeResultDocument.MetaDb
    /// (CompositeResultDocument.MetaDb.cs). Cursor values equal row indexes
    /// because everything lives in one chunk, so the cursor advance is a plain
    /// integer increment instead of Cursor.AddRows; this simplification applies
    /// to both variants. Every append method mirrors the corresponding MetaDb
    /// method byte for byte, including the branch structure of its capacity
    /// checks; branches the benchmark sizes away from taking throw instead of
    /// rolling to a new chunk.
    /// </summary>
    private struct MetaDbClone
    {
        // Cursor.RowBits = 13 (CompositeResultDocument.Cursor.cs lines 25-29).
        private const int RowMask = (1 << 13) - 1;

        private byte[][] _chunks;
        private int _segmentOffset;
        private int _chunkBytes;
        private int _next;

        public static MetaDbClone Create(byte[] chunk)
            => new MetaDbClone
            {
                _chunks = [chunk],
                _segmentOffset = 0,
                _chunkBytes = chunk.Length,
                _next = 0
            };

        public void Reset(int nextValue) => _next = nextValue;

        /// <summary>
        /// Mirrors MetaDb.ReserveRow (CompositeResultDocument.MetaDb.cs lines
        /// 340-379): capacity guard, chunk-boundary check, chunk-table bounds
        /// check, and chunk-buffer null check.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (byte[] Chunk, int ByteOffset, int Cursor) ReserveRow()
        {
            var next = _next;

            // Mirrors the next == Cursor.End guard (MetaDb.cs line 344).
            if (next == int.MaxValue)
            {
                throw new InvalidOperationException("The benchmark chunk is exhausted.");
            }

            var byteOffset = (next & RowMask) * DbRow.Size;

            // Mirrors the chunk roll (MetaDb.cs lines 352-359); the benchmark
            // buffer is sized so this branch is never taken.
            if (byteOffset + DbRow.Size > _chunkBytes)
            {
                throw new InvalidOperationException(
                    "The row does not fit into the benchmark chunk.");
            }

            // Mirrors the chunk-table fetch and rent checks (MetaDb.cs lines 361-376).
            var chunks = _chunks.AsSpan();

            if (chunks.Length < 1)
            {
                throw new InvalidOperationException("The chunk table is empty.");
            }

            var chunk = chunks[0];

            if (chunk is null)
            {
                throw new InvalidOperationException("The chunk buffer is not rented.");
            }

            return (chunk, byteOffset, next);
        }

        /// <summary>
        /// Mirrors MetaDb.Append (CompositeResultDocument.MetaDb.cs lines 49-78)
        /// using the real DbRow constructor for the packing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Append(ElementTokenType tokenType, int location, int parentRow)
        {
            var (chunk, byteOffset, cursor) = ReserveRow();

            var row = new DbRow(tokenType, location: location, parentRow: parentRow);

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, _segmentOffset + byteOffset), row);

            _next = cursor + 1;
            return cursor;
        }

        /// <summary>
        /// Mirrors MetaDb.AppendNull (CompositeResultDocument.MetaDb.cs lines 81-98).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AppendNull(int parentRow)
        {
            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, _segmentOffset + byteOffset);

            // int 0: parent cursor value; ints 1..4 stamped zero via one vector store.
            Unsafe.WriteUnaligned(ref row, parentRow);
            Vector128<byte>.Zero.StoreUnsafe(ref Unsafe.Add(ref row, 4));

            _next = cursor + 1;
            return cursor;
        }

        /// <summary>
        /// Mirrors MetaDb.AppendEmptyProperty (CompositeResultDocument.MetaDb.cs
        /// lines 101-132).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AppendEmptyProperty(int parentRow, int selectionId, ElementFlags flags)
        {
            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, _segmentOffset + byteOffset);

            // int 0: parent cursor value
            Unsafe.WriteUnaligned(ref row, parentRow);

            // int 1: selectionId + opRefType=Selection + flags
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 4),
                selectionId
                | ((int)OperationReferenceType.Selection << 15)
                | (((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift));

            // ints 2..3 must be zero (int 4 is written directly below)
            Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 8), 0, 8);

            // int 4: PropertyName token
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 16),
                (int)ElementTokenType.PropertyName << 15);

            _next = cursor + 1;
            return cursor;
        }

        /// <summary>
        /// Mirrors MetaDb.AppendEmptyPropertyWithNullValue
        /// (CompositeResultDocument.MetaDb.cs lines 135-179) including the
        /// two-row fast-path capacity check.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AppendEmptyPropertyWithNullValue(int parentRow, int selectionId, ElementFlags flags)
        {
            var next = _next;
            var byteOffset = (next & RowMask) * DbRow.Size;
            var chunks = _chunks;

            // Fast path: both rows fit in the current chunk (MetaDb.cs lines 146-173).
            if (byteOffset + (DbRow.Size * 2) <= _chunkBytes
                && 0 < (uint)chunks.Length
                && chunks[0] is { } buffer)
            {
                ref var dest = ref MemoryMarshal.GetArrayDataReference(buffer);
                ref var row0 = ref Unsafe.Add(ref dest, _segmentOffset + byteOffset);

                // Row 0: PropertyName (MetaDb.cs lines 153-164)
                Unsafe.WriteUnaligned(ref row0, parentRow);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref row0, 4),
                    selectionId
                    | ((int)OperationReferenceType.Selection << 15)
                    | (((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift));
                Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row0, 8), 0, 8);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref row0, 16),
                    (int)ElementTokenType.PropertyName << 15);

                // Row 1: None value with parent = cursor value of Row 0
                // (MetaDb.cs lines 166-169)
                ref var row1 = ref Unsafe.Add(ref row0, DbRow.Size);
                Unsafe.WriteUnaligned(ref row1, next);
                Vector128<byte>.Zero.StoreUnsafe(ref Unsafe.Add(ref row1, 4));

                _next = next + 2;
                return next;
            }

            // Slow path (MetaDb.cs lines 175-178); never taken here.
            var propCursor = AppendEmptyProperty(parentRow, selectionId, flags);
            AppendNull(propCursor);
            return propCursor;
        }

        /// <summary>
        /// Mirrors MetaDb.AppendStartObject (CompositeResultDocument.MetaDb.cs
        /// lines 182-217).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AppendStartObject(int parentRow, int selectionSetId, int propertyCount, ElementFlags flags)
        {
            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, _segmentOffset + byteOffset);

            // int 0: parent cursor value
            Unsafe.WriteUnaligned(ref row, parentRow);

            // int 1: selectionSetId + SelectionSet + flags
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 4),
                selectionSetId
                | ((int)OperationReferenceType.SelectionSet << 15)
                | (((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift));

            // int 2: sizeOrLength = property count
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 8), propertyCount);

            // int 3: numberOfRows = (count * 2) + 1
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 12),
                ((propertyCount * 2) + 1) & 0x1FFFFFFF);

            // int 4: StartObject token (sourceDocumentId = 0)
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 16),
                (int)ElementTokenType.StartObject << 15);

            _next = cursor + 1;
            return cursor;
        }

        /// <summary>
        /// Mirrors MetaDb.AppendEndObject (CompositeResultDocument.MetaDb.cs
        /// lines 253-269).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AppendEndObject()
        {
            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, _segmentOffset + byteOffset);

            // int 0: no parent
            Unsafe.WriteUnaligned(ref row, 0);
            // ints 1..3 zeroed (int 4 is written directly below)
            Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 4), 0, 12);
            // int 4: EndObject token
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 16),
                (int)ElementTokenType.EndObject << 15);

            _next = cursor + 1;
            return cursor;
        }

        /// <summary>
        /// The candidate optimization: one whole-block capacity check, one
        /// block copy of the pre-built (2N + 2)-row template, then stamp only
        /// the parent-pointer ints. Row 0 gets the caller's parent, property
        /// rows get the start cursor value, value rows get their property
        /// row's cursor value, and the EndObject parent stays 0 (already 0 in
        /// the template).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendTemplateBlock(byte[] template, int parentRow)
        {
            var next = _next;
            var byteOffset = (next & RowMask) * DbRow.Size;
            var chunks = _chunks;

            if (byteOffset + template.Length <= _chunkBytes
                && 0 < (uint)chunks.Length
                && chunks[0] is { } buffer)
            {
                ref var dest = ref Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(buffer),
                    _segmentOffset + byteOffset);

                Unsafe.CopyBlockUnaligned(
                    ref dest,
                    ref MemoryMarshal.GetArrayDataReference(template),
                    (uint)template.Length);

                // Stamp the StartObject parent.
                Unsafe.WriteUnaligned(ref dest, parentRow);

                // Stamp the property/value parents; the trailing EndObject row
                // keeps its template parent of 0.
                var rows = template.Length / DbRow.Size;

                for (var rowOffset = 1; rowOffset < rows - 1; rowOffset += 2)
                {
                    Unsafe.WriteUnaligned(
                        ref Unsafe.Add(ref dest, rowOffset * DbRow.Size),
                        next);
                    Unsafe.WriteUnaligned(
                        ref Unsafe.Add(ref dest, (rowOffset + 1) * DbRow.Size),
                        next + rowOffset);
                }

                _next = next + rows;
                return;
            }

            // The real implementation would fall back to the per-row append
            // loop here (chunk boundary or unrented chunk); the benchmark is
            // sized so this cannot happen.
            throw new InvalidOperationException(
                "The template block does not fit into the benchmark chunk.");
        }
    }
}
