using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Compares the current <c>SourceResultDocument.MetaDb.Get</c> and
/// <c>GetJsonTokenType</c> read path (chunk table indexing plus span slice plus
/// <c>MemoryMarshal.Read</c>) against a candidate that reads the same chunked row
/// layout through <c>MemoryMarshal.GetArrayDataReference</c> and
/// <c>Unsafe.ReadUnaligned</c>, the pattern the class already uses for writes.
///
/// Each invocation decodes every MetaDb row of a realistic parsed subgraph payload
/// (150 product objects with 8 scalar properties each, about 2,700 rows spanning
/// six chunks of the geometric chunk schedule), performing one full row read and
/// one token-type read per row, which mirrors the read mix of the
/// BuildResult/TryComplete recursion.
///
/// The baseline calls the real product methods on a real <c>MetaDb</c> instance.
/// The parsed document keeps its own MetaDb in a private field, so the baseline
/// instance is populated by replaying the parsed rows through the real
/// <c>MetaDb.Append</c>, which writes bit-identical row bytes. The benchmark-local
/// variants (faithful copy and optimized candidate) read a replica chunk table
/// built with the exact same chunk schedule and row bytes; GlobalSetup verifies
/// every row decoded by every variant against the parsed document.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class MetaDbGetBenchmark
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

    private const int ItemCount = 150;

#pragma warning disable IDE0370 // Remove unnecessary suppression
    private MemoryArena _arena = null!;
    private SourceResultDocument _document = null!;
    private SourceResultDocument.MetaDb _metaDb;
    private MemorySegment[] _chunks = null!;
    private SourceResultDocument.Cursor[] _cursors = null!;
    private SourceResultDocument.DbRow[] _expectedRows = null!;
#pragma warning restore IDE0370 // Remove unnecessary suppression

    public long Consumed;

    [GlobalSetup]
    public void Setup()
    {
        _arena = new MemoryArena();

        var payload = CreatePayload();
        _document = SourceResultDocument.Parse(_arena, payload, payload.Length);

        // The root StartObject row spans the whole document, so its row count is the
        // total number of MetaDb rows including the trailing EndObject row.
        var totalRows = _document.GetDbRow(SourceResultDocument.Cursor.Zero).NumberOfRows;

        _cursors = new SourceResultDocument.Cursor[totalRows];
        _expectedRows = new SourceResultDocument.DbRow[totalRows];

        var cursor = SourceResultDocument.Cursor.Zero;

        for (var i = 0; i < totalRows; i++)
        {
            _cursors[i] = cursor;
            _expectedRows[i] = _document.GetDbRow(cursor);
            cursor += 1;
        }

        // The parsed document holds its MetaDb in the private _parsedData field with no
        // internal accessor, so the baseline replays the parsed rows into a MetaDb owned
        // by this benchmark. Append constructs and writes the exact DbRow bytes
        // (SourceResultDocument.MetaDb.cs lines 99-113), and the DbRow constructor
        // round-trips (TokenType, Location, SizeOrLength, NumberOfRows, HasComplexChildren)
        // bit-identically, which Verify() checks row by row.
        _metaDb = SourceResultDocument.MetaDb.CreateForEstimatedRows(_arena, totalRows);

        for (var i = 0; i < totalRows; i++)
        {
            var row = _expectedRows[i];
            _metaDb.Append(
                row.TokenType,
                row.Location,
                row.SizeOrLength,
                row.NumberOfRows,
                row.HasComplexChildren);
        }

        // Replica of the MetaDb chunk table for the benchmark-local Get variants. The
        // chunk sizes follow the exact schedule MetaDb uses when renting chunks
        // (SourceResultDocument.MetaDb.cs lines 55 and 168) and the rows are written
        // exactly as Append writes them (lines 108-110).
        var chunkCount = _cursors[totalRows - 1].Chunk + 1;
        _chunks = new MemorySegment[chunkCount];

        for (var i = 0; i < chunkCount; i++)
        {
            _chunks[i] = _arena.Rent(1 << (10 + (int)SourceResultDocument.Cursor.ChunkSizeFor(i)));
        }

        for (var i = 0; i < totalRows; i++)
        {
            var target = _cursors[i];
            var chunk = _chunks[target.Chunk];
            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref dest, chunk.Offset + target.ByteOffset),
                _expectedRows[i]);
        }

        Verify();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _document.Dispose();
        _metaDb.Dispose();
        _arena.Dispose();
    }

    /// <summary>
    /// Current product behavior: the real <c>MetaDb.Get</c> and
    /// <c>MetaDb.GetJsonTokenType</c> called through internals on a real MetaDb.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long GetRows_Product()
    {
        var cursors = _cursors;
        var sum = 0L;

        for (var i = 0; i < cursors.Length; i++)
        {
            var cursor = cursors[i];
            var row = _metaDb.Get(cursor);
            sum += row.Location + row.SizeOrLength + row.NumberOfRows + (int)row.TokenType;
            sum += (int)_metaDb.GetJsonTokenType(cursor);
        }

        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// Byte-faithful benchmark-local copy of the current read path running over the
    /// replica chunk table. Its delta against the optimized variant isolates the
    /// candidate on identical memory; its delta against the product baseline shows
    /// the replica is equivalent.
    /// </summary>
    [Benchmark]
    public long GetRows_SpanReadCopy()
    {
        var cursors = _cursors;
        var chunks = _chunks;
        var sum = 0L;

        for (var i = 0; i < cursors.Length; i++)
        {
            var cursor = cursors[i];
            var row = GetRowSpanRead(chunks, cursor);
            sum += row.Location + row.SizeOrLength + row.NumberOfRows + (int)row.TokenType;
            sum += (int)GetJsonTokenTypeSpanRead(chunks, cursor);
        }

        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// Candidate optimization: chunk table and chunk buffer accessed through
    /// <c>MemoryMarshal.GetArrayDataReference</c> plus <c>Unsafe.ReadUnaligned</c>,
    /// removing the bounds checks, the span construction, and the second length
    /// guard of <c>MemoryMarshal.Read</c>.
    /// </summary>
    [Benchmark]
    public long GetRows_ArrayDataRefReadUnaligned()
    {
        var cursors = _cursors;
        var chunks = _chunks;
        var sum = 0L;

        for (var i = 0; i < cursors.Length; i++)
        {
            var cursor = cursors[i];
            var row = GetRowArrayDataRef(chunks, cursor);
            sum += row.Location + row.SizeOrLength + row.NumberOfRows + (int)row.TokenType;
            sum += (int)GetJsonTokenTypeArrayDataRef(chunks, cursor);
        }

        Consumed = sum;
        return sum;
    }

    // Byte-faithful copy of SourceResultDocument.MetaDb.Get,
    // src/HotChocolate/Fusion/src/Fusion.Execution/Text/Json/SourceResultDocument.MetaDb.cs
    // lines 183-192. AssertValidCursor is [Conditional("DEBUG")] and compiles away in the
    // release builds benchmarks run under, so it is omitted here.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SourceResultDocument.DbRow GetRowSpanRead(
        MemorySegment[] chunks,
        SourceResultDocument.Cursor cursor)
    {
        var chunk = chunks[cursor.Chunk];
        var dataPos = chunk.Buffer.AsSpan(chunk.Offset + cursor.ByteOffset);

        return MemoryMarshal.Read<SourceResultDocument.DbRow>(dataPos);
    }

    // Byte-faithful copy of SourceResultDocument.MetaDb.GetJsonTokenType,
    // src/HotChocolate/Fusion/src/Fusion.Execution/Text/Json/SourceResultDocument.MetaDb.cs
    // lines 194-205 (same DEBUG-only assert note as above).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JsonTokenType GetJsonTokenTypeSpanRead(
        MemorySegment[] chunks,
        SourceResultDocument.Cursor cursor)
    {
        // _numberOfRowsAndTypeUnion is the third int in the row.
        var chunk = chunks[cursor.Chunk];
        var dataPos = chunk.Buffer.AsSpan(chunk.Offset + cursor.ByteOffset + 8);

        var union = MemoryMarshal.Read<uint>(dataPos);
        return (JsonTokenType)(union >> 28);
    }

    // Optimized variant of MetaDb.Get: the pattern MetaDb.Append (lines 109-110) and
    // SourceResultDocument.ReadRawValue (lines 390-400) already use on their hot paths.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SourceResultDocument.DbRow GetRowArrayDataRef(
        MemorySegment[] chunks,
        SourceResultDocument.Cursor cursor)
    {
        ref readonly var chunk = ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(chunks),
            cursor.Chunk);

        return Unsafe.ReadUnaligned<SourceResultDocument.DbRow>(
            ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(chunk.Buffer),
                chunk.Offset + cursor.ByteOffset));
    }

    // Optimized variant of MetaDb.GetJsonTokenType: reads only the third int of the row.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JsonTokenType GetJsonTokenTypeArrayDataRef(
        MemorySegment[] chunks,
        SourceResultDocument.Cursor cursor)
    {
        ref readonly var chunk = ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(chunks),
            cursor.Chunk);

        var union = Unsafe.ReadUnaligned<uint>(
            ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(chunk.Buffer),
                chunk.Offset + cursor.ByteOffset + 8));

        return (JsonTokenType)(union >> 28);
    }

    private void Verify()
    {
        for (var i = 0; i < _cursors.Length; i++)
        {
            var cursor = _cursors[i];
            var expected = _expectedRows[i];

            AssertRowEqual(expected, _metaDb.Get(cursor), i, "product MetaDb.Get");
            AssertRowEqual(expected, GetRowSpanRead(_chunks, cursor), i, "span-read copy Get");
            AssertRowEqual(expected, GetRowArrayDataRef(_chunks, cursor), i, "optimized Get");

            var productToken = _metaDb.GetJsonTokenType(cursor);
            var copyToken = GetJsonTokenTypeSpanRead(_chunks, cursor);
            var optimizedToken = GetJsonTokenTypeArrayDataRef(_chunks, cursor);

            if (productToken != expected.TokenType
                || copyToken != expected.TokenType
                || optimizedToken != expected.TokenType)
            {
                throw new InvalidOperationException(
                    $"Token type mismatch at row {i}: expected {expected.TokenType}, "
                    + $"product {productToken}, copy {copyToken}, optimized {optimizedToken}.");
            }
        }

        var productSum = GetRows_Product();
        var copySum = GetRows_SpanReadCopy();
        var optimizedSum = GetRows_ArrayDataRefReadUnaligned();

        if (productSum != copySum || productSum != optimizedSum)
        {
            throw new InvalidOperationException(
                $"Checksum mismatch: product {productSum}, copy {copySum}, "
                + $"optimized {optimizedSum}.");
        }
    }

    private static void AssertRowEqual(
        in SourceResultDocument.DbRow expected,
        in SourceResultDocument.DbRow actual,
        int rowIndex,
        string variant)
    {
        if (expected.TokenType != actual.TokenType
            || expected.Location != actual.Location
            || expected.SizeOrLength != actual.SizeOrLength
            || expected.NumberOfRows != actual.NumberOfRows
            || expected.HasComplexChildren != actual.HasComplexChildren)
        {
            throw new InvalidOperationException(
                $"Row {rowIndex} mismatch in {variant}: expected "
                + $"({expected.TokenType}, {expected.Location}, {expected.SizeOrLength}, "
                + $"{expected.NumberOfRows}, {expected.HasComplexChildren}) but got "
                + $"({actual.TokenType}, {actual.Location}, {actual.SizeOrLength}, "
                + $"{actual.NumberOfRows}, {actual.HasComplexChildren}).");
        }
    }

    private static byte[] CreatePayload()
    {
        var json = new StringBuilder(32 * 1024);
        json.Append("{\"data\":{\"products\":{\"nodes\":[");

        for (var i = 0; i < ItemCount; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            json.Append("{\"id\":\"prod-").Append(i).Append('"');
            json.Append(",\"name\":\"Product ").Append(i).Append('"');
            json.Append(",\"price\":").Append(i).Append(".99");
            json.Append(",\"inStock\":").Append(i % 3 == 0 ? "false" : "true");
            json.Append(",\"quantity\":").Append(i % 50);
            json.Append(",\"rating\":4.5");

            if (i % 7 == 0)
            {
                json.Append(",\"description\":null");
            }
            else if (i % 10 == 0)
            {
                // Escaped content marks the string row with HasComplexChildren.
                json.Append(",\"description\":\"A \\\"quoted\\\" description\"");
            }
            else
            {
                json.Append(",\"description\":\"Plain description for item ").Append(i).Append('"');
            }

            json.Append(",\"sku\":\"SKU-").Append(i).Append("\"}");
        }

        json.Append("]}}}");
        return Encoding.UTF8.GetBytes(json.ToString());
    }
}
