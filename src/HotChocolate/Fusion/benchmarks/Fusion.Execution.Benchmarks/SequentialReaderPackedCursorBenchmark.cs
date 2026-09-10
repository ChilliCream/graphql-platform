using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;
using CompositeCursor = HotChocolate.Fusion.Text.Json.CompositeResultDocument.Cursor;
using DbRow = HotChocolate.Fusion.Text.Json.CompositeResultDocument.DbRow;
using ElementFlags = HotChocolate.Fusion.Text.Json.CompositeResultDocument.ElementFlags;
using PropertyMetadata =
    HotChocolate.Fusion.Text.Json.CompositeResultDocument.MetaDb.PropertyMetadata;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures the navigation cost of the composite MetaDb <c>SequentialReader</c>, the
/// per-row engine of response serialization (RawJsonFormatter.WriteObject/WriteArray,
/// CompositeResultDocument.WriteTo.cs lines 133-212).
///
/// Per serialized property the current reader pays: a <c>Cursor</c> getter that repacks
/// (chunk, row) through <c>Cursor.From</c> with two throw-checked range validations and
/// a <c>RowsPerChunkFor</c> recomputation (Cursor.cs lines 74-87 and 60-68), and an
/// <c>Advance</c> whose in-chunk fast path recomputes <c>_chunkLength / DbRow.Size</c>
/// (MetaDb.cs lines 722-735), even though the position is valid by construction: the
/// reader only moves via MoveTo/Advance over rows it already bounds-checked.
///
/// The candidate keeps the packed cursor value and the chunk row capacity as reader
/// fields, set in MoveTo: Advance's fast path becomes a packed increment (safe: the row
/// occupies the low 13 bits and rows-per-chunk maxes at 6553, and the fast path only
/// fires when nextRow stays below the chunk capacity) compared against the cached
/// capacity, and the Cursor getter becomes the trusting <c>new Cursor(packed)</c>
/// (Cursor.cs line 54).
///
/// Both kernels replicate the WriteObject reader sequence (ReadProperty, Cursor,
/// PeekRow, Advance including the internal/excluded branch) over the same materialized
/// document without the JsonWriter, so the delta isolates pure reader navigation. The
/// baseline drives the real product <c>MetaDb.SequentialReader</c>; the candidate is a
/// byte-faithful local replica differing only in the tracked state. The 500-object
/// corpus spans the geometric chunk ramp, so chunk-roll MoveTo paths execute in both.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class SequentialReaderPackedCursorBenchmark : FusionBenchmarkBase
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
    private const int ObjectCount = 500;

    /// <summary>
    /// Fields per object: 9 exercises a wide serialization row run, 2 the tiny-object
    /// shape where the fixed per-object reader setup dominates.
    /// </summary>
    [Params(9, 2)]
    public int Width = 9;

    private MemoryArena _arena = null!;
    private CompositeResultDocument _document = null!;
    private MemorySegment[] _chunks = null!;
    private CompositeCursor[] _objectStarts = null!;
    private int[] _objectRowCounts = null!;

    public long Consumed;

    [GlobalSetup]
    public void Setup()
    {
        var schema = CreateFusionSchema();
        var documentRewriter = new DocumentRewriter(schema);

        // The products.nodes selection set has 9 direct fields; the dimension
        // selection set has 2. Both compile from one operation.
        var operationDefinition = documentRewriter
            .RewriteDocument(
                Utf8GraphQLParser.Parse(
                    """
                    {
                      products {
                        nodes {
                          id
                          name
                          description
                          price
                          averageRating
                          title
                          estimatedDelivery
                          dimension { height width }
                          reviews { nodes { id body stars } }
                        }
                      }
                      productById(id: "1") {
                        dimension { height width }
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
        var operation = operationCompiler.Compile(
            OperationId,
            OperationId,
            OperationId,
            operationDefinition);

        _arena = new MemoryArena();
        _document = new CompositeResultDocument(_arena, operation, includeFlags: 0);

        var rootContext = _document.Data.GetObjectContext();

        SelectionSet elementSelectionSet;
        CompositeResultElement listSlot;

        if (Width == 9)
        {
            var productsSelection = operation.RootSelectionSet.Selections[0];
            var connectionSelectionSet = operation.GetSelectionSet(productsSelection);

            if (!connectionSelectionSet.TryGetSelection("nodes"u8, out var nodesSelection))
            {
                throw new InvalidOperationException("The nodes selection is missing.");
            }

            elementSelectionSet = operation.GetSelectionSet(nodesSelection);

            if (!rootContext.TryGetProperty("products"u8, out var productsSlot, out _))
            {
                throw new InvalidOperationException("The products slot is missing.");
            }

            productsSlot.SetObjectValue(connectionSelectionSet, out var connectionContext);

            if (!connectionContext.TryGetProperty("nodes"u8, out listSlot, out _))
            {
                throw new InvalidOperationException("The nodes slot is missing.");
            }
        }
        else
        {
            var productSelection = operation.RootSelectionSet.Selections[1];
            var productSelectionSet = operation.GetSelectionSet(productSelection);

            if (!productSelectionSet.TryGetSelection("dimension"u8, out var dimensionSelection))
            {
                throw new InvalidOperationException("The dimension selection is missing.");
            }

            elementSelectionSet = operation.GetSelectionSet(dimensionSelection);

            if (!rootContext.TryGetProperty("productById"u8, out var productSlot, out _))
            {
                throw new InvalidOperationException("The productById slot is missing.");
            }

            productSlot.SetObjectValue(productSelectionSet, out var productContext);

            if (!productContext.TryGetProperty("dimension"u8, out listSlot, out _))
            {
                throw new InvalidOperationException("The dimension slot is missing.");
            }
        }

        if (elementSelectionSet.Selections.Length != Width)
        {
            throw new InvalidOperationException(
                $"The element selection set has {elementSelectionSet.Selections.Length} "
                + $"selections instead of {Width}.");
        }

        // A single array whose elements are materialized objects: the exact shape
        // RawJsonFormatter.WriteArray/WriteObject serializes, spanning multiple chunks.
        listSlot.SetArrayValue(ObjectCount);

        _objectStarts = new CompositeCursor[ObjectCount];
        _objectRowCounts = new int[ObjectCount];
        var i = 0;

        foreach (var element in listSlot.EnumerateArray())
        {
            element.SetObjectValue(elementSelectionSet, out var elementContext);

            if (!elementContext.TryGetProperty(
                elementSelectionSet.Selections[0].Utf8ResponseName,
                out _,
                out _))
            {
                throw new InvalidOperationException("Object materialization failed.");
            }

            i++;
        }

        if (i != ObjectCount)
        {
            throw new InvalidOperationException("Object materialization is incomplete.");
        }

        // Resolve each element's object start row exactly like the serializer does:
        // the array element rows are references to the object blocks.
        var metaDbField = typeof(CompositeResultDocument).GetField(
            "_metaDb",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("The _metaDb field is missing.");
        var boxedMetaDb = metaDbField.GetValue(_document)
            ?? throw new InvalidOperationException("The MetaDb value is missing.");
        var chunksField = boxedMetaDb.GetType().GetField(
            "_chunks",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("The _chunks field is missing.");
        _chunks = (MemorySegment[])chunksField.GetValue(boxedMetaDb)!;

        i = 0;

        foreach (var element in listSlot.EnumerateArray())
        {
            var cursor = element.Cursor;
            var row = _document._metaDb.GetValue(ref cursor);

            if (row.TokenType is not ElementTokenType.StartObject)
            {
                throw new InvalidOperationException(
                    $"Element {i} resolves to {row.TokenType} instead of an object.");
            }

            _objectStarts[i] = cursor;
            _objectRowCounts[i] = row.NumberOfRows;
            i++;
        }

        if (_objectStarts[^1].Chunk == _objectStarts[0].Chunk)
        {
            throw new InvalidOperationException(
                "The corpus fits one chunk; chunk-roll paths would go unexercised.");
        }

        VerifyEquivalence();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _document.Dispose();
        _arena.Dispose();
    }

    private void VerifyEquivalence()
    {
        for (var i = 0; i < _objectStarts.Length; i++)
        {
            var baseline = NavigateObjectBaseline(_objectStarts[i], _objectRowCounts[i]);
            var packed = NavigateObjectPacked(_objectStarts[i], _objectRowCounts[i]);

            if (baseline != packed)
            {
                throw new InvalidOperationException(
                    $"Navigation checksum mismatch on object {i}: "
                    + $"baseline {baseline} vs packed {packed}.");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public long Navigate_Product()
    {
        var sum = 0L;

        for (var i = 0; i < _objectStarts.Length; i++)
        {
            sum += NavigateObjectBaseline(_objectStarts[i], _objectRowCounts[i]);
        }

        Consumed = sum;
        return sum;
    }

    [Benchmark]
    public long Navigate_PackedCursor()
    {
        var sum = 0L;

        for (var i = 0; i < _objectStarts.Length; i++)
        {
            sum += NavigateObjectPacked(_objectStarts[i], _objectRowCounts[i]);
        }

        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// The reader sequence of RawJsonFormatter.WriteObject (WriteTo.cs lines 148-179)
    /// against the real product SequentialReader, with the JsonWriter calls replaced
    /// by checksum consumption so only navigation is measured.
    /// </summary>
    private long NavigateObjectBaseline(CompositeCursor start, int numberOfRows)
    {
        var remainingRows = numberOfRows - 1;
        var sum = 0L;

        var reader = _document._metaDb.CreateSequentialReader(start + 1);

        while (remainingRows > 0)
        {
            var property = reader.ReadProperty();

            if ((ElementFlags.IsInternal & property.Flags) == ElementFlags.IsInternal
                || (ElementFlags.IsExcluded & property.Flags) == ElementFlags.IsExcluded)
            {
                remainingRows -= 2;

                if (remainingRows > 0)
                {
                    reader.Advance(1);
                }

                continue;
            }

            sum += property.SelectionId;

            var row = reader.PeekRow();
            sum += reader.Cursor.Value + (int)row.TokenType;
            remainingRows -= 2;

            if (remainingRows > 0)
            {
                reader.Advance(1);
            }
        }

        return sum;
    }

    /// <summary>
    /// The identical sequence against the packed-cursor candidate replica.
    /// </summary>
    private long NavigateObjectPacked(CompositeCursor start, int numberOfRows)
    {
        var remainingRows = numberOfRows - 1;
        var sum = 0L;

        var reader = new PackedSequentialReader(_chunks, start + 1);

        while (remainingRows > 0)
        {
            var property = reader.ReadProperty();

            if ((ElementFlags.IsInternal & property.Flags) == ElementFlags.IsInternal
                || (ElementFlags.IsExcluded & property.Flags) == ElementFlags.IsExcluded)
            {
                remainingRows -= 2;

                if (remainingRows > 0)
                {
                    reader.Advance(1);
                }

                continue;
            }

            sum += property.SelectionId;

            var row = reader.PeekRow();
            sum += reader.Cursor.Value + (int)row.TokenType;
            remainingRows -= 2;

            if (remainingRows > 0)
            {
                reader.Advance(1);
            }
        }

        return sum;
    }

    /// <summary>
    /// Candidate reader: byte-faithful copy of MetaDb.SequentialReader (MetaDb.cs lines
    /// 674-750) except MoveTo also stores the packed cursor value and the chunk's row
    /// capacity, Advance's fast path increments the packed value and compares against
    /// the cached capacity (no division), and the Cursor getter is the trusting
    /// single-int constructor (no From revalidation). The packed increment cannot carry
    /// into the chunk bits: rows-per-chunk maxes at 6553 against the 13-bit row field
    /// and the fast path only fires below the chunk capacity.
    /// </summary>
    private ref struct PackedSequentialReader
    {
        private readonly MemorySegment[] _chunks;
        private ref byte _chunkBase;
        private int _packedCursor;
        private int _row;
        private int _rowsPerChunk;
        private int _byteOffset;
        private int _segmentOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PackedSequentialReader(MemorySegment[] chunks, CompositeCursor cursor)
        {
            _chunks = chunks;
            _chunkBase = ref Unsafe.NullRef<byte>();
            _packedCursor = 0;
            _row = 0;
            _rowsPerChunk = 0;
            _byteOffset = 0;
            _segmentOffset = 0;
            MoveTo(cursor);
        }

        internal readonly CompositeCursor Cursor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_packedCursor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly DbRow PeekRow()
            => Unsafe.ReadUnaligned<DbRow>(
                ref Unsafe.Add(ref _chunkBase, _segmentOffset + _byteOffset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PropertyMetadata ReadProperty()
        {
            var selectionAndFlags = Unsafe.ReadUnaligned<int>(
                ref Unsafe.Add(
                    ref _chunkBase,
                    _segmentOffset + _byteOffset + DbRow.SelectionAndFlagsOffset));
            var property = new PropertyMetadata(selectionAndFlags);

            Advance(1);
            return property;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Advance(int rowCount)
        {
            Debug.Assert(rowCount > 0);
            var nextRow = _row + rowCount;

            if ((uint)nextRow < (uint)_rowsPerChunk)
            {
                _row = nextRow;
                _packedCursor += rowCount;
                _byteOffset += rowCount * DbRow.Size;
                return;
            }

            MoveTo(new CompositeCursor(_packedCursor).AddRows(rowCount));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveTo(CompositeCursor cursor)
        {
            var segment = _chunks[cursor.Chunk];
            Debug.Assert(segment.Buffer is not null);

            _packedCursor = cursor.Value;
            _row = cursor.Row;
            _rowsPerChunk = segment.Length / DbRow.Size;
            _byteOffset = cursor.ByteOffset;
            _segmentOffset = segment.Offset;
            _chunkBase = ref MemoryMarshal.GetArrayDataReference(segment.Buffer);
        }
    }
}
