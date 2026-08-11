using System;
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
/// Isolates the duplicate MetaDb row decodes of source object and array enumeration on the
/// FetchResultStore.SaveSafeResult -> ValueCompletion.BuildResult -> TryComplete* hot path.
///
/// Today each visited property or element pays two identical row decodes: the loop body
/// snapshots the value via CreateSnapshot -> GetValueRow (SourceResultElement.cs line 534,
/// called from ValueCompletion.cs lines 129, 177, 976, 1165 and 1206) and the advancing
/// MoveNext decodes the same row again to compute the skip to the next value
/// (SourceResultElement.ObjectEnumerator.cs line 116, SourceResultElement.ArrayEnumerator.cs
/// line 105). Entering an object or array pays two further container row reads even when the
/// caller already holds the decoded row in a snapshot: the token-type guard
/// (SourceResultElement.cs lines 600 and 623) and GetEndIndex inside the enumerator
/// constructor (SourceResultDocument.Text.cs line 311, reached from ObjectEnumerator.cs
/// line 25 and ArrayEnumerator.cs line 25). SourceResultElementSnapshot.EnumerateArray and
/// EnumerateObject (SourceResultElementSnapshot.cs lines 469 and 477) delegate to the element
/// and pay both container reads despite holding the row.
///
/// The baseline replays this sequence with the real product enumerators and CreateSnapshot.
/// The optimized variant is a benchmark-local copy of the recommended row-carrying design:
/// MoveNext decodes each value row once on arrival and caches it, the advancing MoveNext
/// computes the skip from the cached row, the property struct carries the row into the loop
/// body, and snapshot-built enumerators derive the end cursor from the cached container row
/// (endCursor = cursor + (NumberOfRows - 1), mirroring SourceResultDocument.Text.cs
/// lines 319-320) with zero row reads. Every row decode is the same two dependent loads
/// (SourceResultDocument.MetaDb.cs lines 184-196), so the variants differ only in decode
/// counts. Property name reads and selection matching are factored out of both variants
/// because the design leaves the name path unchanged.
///
/// Shapes: Dense1000 is a 1000-element array of 7-property objects with nested containers
/// whose MetaDb spans multiple chunks (the likely cache-miss case). Tiny1000 (1-property
/// objects) and Empty1000 (empty objects) bound the enumerator and property struct growth
/// risk flagged in the verification. Single1 is the N=1 shape.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class SourceEnumeratorRowCarryBenchmark
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

    private const string DenseShape = "Dense1000";
    private const string TinyShape = "Tiny1000";
    private const string EmptyShape = "Empty1000";
    private const string SingleShape = "Single1";

    private const long ChecksumSeed = 17L;

    private static readonly string[] s_shapes = [DenseShape, TinyShape, EmptyShape, SingleShape];

    private MemoryArena _arena = null!;
    private SourceResultDocument[] _documents = null!;
    private SourceResultDocument _document = null!;

    [Params(DenseShape, TinyShape, EmptyShape, SingleShape)]
    public string Shape { get; set; } = DenseShape;

    [GlobalSetup]
    public void Setup()
    {
        _arena = new MemoryArena();
        _documents = new SourceResultDocument[s_shapes.Length];

        for (var i = 0; i < s_shapes.Length; i++)
        {
            var json = BuildPayload(s_shapes[i]);
            _documents[i] = SourceResultDocument.Parse(_arena, json, json.Length);

            // The equivalence check runs over every shape, not just the measured one. The
            // checksum is an order-sensitive rolling hash over (cursor, token type, location,
            // size) of every visited value, so equality proves both variants visit the same
            // rows in the same order and decode identical content.
            var baseline = BaselineWalk(_documents[i]);
            var optimized = OptimizedWalk(_documents[i]);

            if (baseline != optimized)
            {
                throw new InvalidOperationException(
                    $"Shape '{s_shapes[i]}': baseline checksum {baseline} differs from "
                    + $"row-carrying checksum {optimized}.");
            }

            if (baseline == ChecksumSeed)
            {
                throw new InvalidOperationException(
                    $"Shape '{s_shapes[i]}': the walk visited no values.");
            }
        }

        _document = _documents[Array.IndexOf(s_shapes, Shape)];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        for (var i = 0; i < _documents.Length; i++)
        {
            _documents[i].Dispose();
        }

        _arena.Dispose();
    }

    /// <summary>
    /// Current product enumeration sequence using only real product internals: the root
    /// snapshot mirrors ValueCompletion.BuildResult line 60, EnumerateObject/EnumerateArray
    /// pay the guard read plus the constructor GetEndIndex read, per value CreateSnapshot
    /// decodes the row the enumerator's MoveNext decodes again to advance.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long CurrentEnumerationDoubleDecode() => BaselineWalk(_document);

    /// <summary>
    /// Candidate optimization: benchmark-local row-carrying enumerators decode each value row
    /// once on arrival, and snapshot-built construction performs zero container row reads.
    /// </summary>
    [Benchmark]
    public long RowCarryingEnumerationSingleDecode() => OptimizedWalk(_document);

    private static long BaselineWalk(SourceResultDocument document)
        => BaselineWalkValue(ChecksumSeed, document.Root.CreateSnapshot());

    private static long OptimizedWalk(SourceResultDocument document)
        => OptimizedWalkValue(ChecksumSeed, document.Root.CreateSnapshot());

    private static long Accumulate(long checksum, SourceResultElementSnapshot value)
    {
        // The row is cached on the snapshot, so reading it here is free in both variants.
        // Consuming cursor, token type, location and size keeps the decoded row live and
        // makes the checksum order-sensitive and content-sensitive.
        var row = value.GetValueRow();

        unchecked
        {
            checksum = (checksum * 31) + value._cursor.Value;
            checksum = (checksum * 31) + (int)row.TokenType;
            checksum = (checksum * 31) + row.Location;
            checksum = (checksum * 31) + row.SizeOrLength;
        }

        return checksum;
    }

    private static long BaselineWalkValue(long checksum, SourceResultElementSnapshot value)
    {
        checksum = Accumulate(checksum, value);

        // The kind dispatch mirrors the ValueCompletion loop bodies; it reads the cached
        // snapshot row and costs the same in both variants.
        var kind = value.ValueKind;

        if (kind is JsonValueKind.Object)
        {
            // Mirrors the property loops at ValueCompletion.cs lines 170-177 and 1199-1206:
            // EnumerateObject pays the token-type guard plus GetEndIndex, each property pays
            // CreateSnapshot's GetValueRow plus the advancing MoveNext's GetDbRow.
            foreach (var property in value.EnumerateObject())
            {
                checksum = BaselineWalkValue(checksum, property.Value.CreateSnapshot());
            }
        }
        else if (kind is JsonValueKind.Array)
        {
            // Mirrors the element loop at ValueCompletion.cs lines 958-976.
            foreach (var element in value.EnumerateArray())
            {
                checksum = BaselineWalkValue(checksum, element.CreateSnapshot());
            }
        }

        return checksum;
    }

    private static long OptimizedWalkValue(long checksum, SourceResultElementSnapshot value)
    {
        checksum = Accumulate(checksum, value);

        var kind = value.ValueKind;

        if (kind is JsonValueKind.Object)
        {
            foreach (var property in EnumerateObjectRowCarry(value))
            {
                checksum = OptimizedWalkValue(checksum, property.CreateValueSnapshot());
            }
        }
        else if (kind is JsonValueKind.Array)
        {
            foreach (var elementSnapshot in EnumerateArrayRowCarry(value))
            {
                checksum = OptimizedWalkValue(checksum, elementSnapshot);
            }
        }

        return checksum;
    }

    // Stands in for the proposed SourceResultElementSnapshot.EnumerateObject overload: the
    // guard checks the cached row's token type instead of re-reading it (the product version
    // must throw exactly the exceptions SourceResultElement.cs lines 619-629 throws today)
    // and the enumerator constructor performs zero row reads.
    private static RowCarryObjectEnumerator EnumerateObjectRowCarry(
        SourceResultElementSnapshot snapshot)
    {
        if (snapshot._parent is null || snapshot._row.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException();
        }

        return new RowCarryObjectEnumerator(snapshot._parent, snapshot._cursor, snapshot._row);
    }

    // Stands in for the proposed SourceResultElementSnapshot.EnumerateArray overload
    // (product version replicates the formatted message and Source marker of
    // SourceResultElement.cs lines 596-611).
    private static RowCarryArrayEnumerator EnumerateArrayRowCarry(
        SourceResultElementSnapshot snapshot)
    {
        if (snapshot._parent is null || snapshot._row.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException();
        }

        return new RowCarryArrayEnumerator(snapshot._parent, snapshot._cursor, snapshot._row);
    }

    /// <summary>
    /// Benchmark-local copy of the row-carrying variant of
    /// SourceResultElement.ObjectEnumerator (ObjectEnumerator.cs lines 20-133): identical
    /// cursor arithmetic and bounds checks, but the row is decoded once on arrival and the
    /// advance uses the cached row instead of re-reading it (line 116 today). Construction
    /// derives the end cursor from the caller's cached container row instead of GetEndIndex.
    /// </summary>
    private struct RowCarryObjectEnumerator
    {
        private readonly SourceResultDocument _parent;
        private readonly SourceResultDocument.Cursor _containerCursor;
        private readonly SourceResultDocument.Cursor _endCursor;
        private SourceResultDocument.Cursor _current;
        private SourceResultDocument.DbRow _row;
        private bool _hasStarted;

        internal RowCarryObjectEnumerator(
            SourceResultDocument parent,
            SourceResultDocument.Cursor containerCursor,
            SourceResultDocument.DbRow containerRow)
        {
            _parent = parent;
            _containerCursor = containerCursor;

            // GetEndIndex(cursor, includeEndElement: false) for a composite value is
            // cursor + (NumberOfRows - 1) (SourceResultDocument.Text.cs lines 319-320),
            // computable from the cached row without a MetaDb read.
            _endCursor = containerCursor + (containerRow.NumberOfRows - 1);

            _current = default;
            _row = default;
            _hasStarted = false;
        }

        public readonly RowCarryProperty Current
        {
            get
            {
                if (!_hasStarted)
                {
                    return default;
                }

                return new RowCarryProperty(_parent, _current, _row);
            }
        }

        public readonly RowCarryObjectEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (!_hasStarted)
            {
                // First property: after StartObject comes PropertyName (+1), then Value (+1).
                // Identical arithmetic and bounds check as ObjectEnumerator.cs lines 94-110;
                // the only change is the arrival decode of the row the loop body will use.
                var firstName = _containerCursor + 1;
                var firstValue = firstName + 1;

                if (firstValue < _endCursor)
                {
                    _current = firstValue;
                    _row = _parent.GetDbRow(firstValue);
                    _hasStarted = true;
                    return true;
                }

                _current = _endCursor;
                _hasStarted = false;
                return false;
            }

            // The skip past the current value uses the cached row (decoded on arrival)
            // instead of the departure GetDbRow at ObjectEnumerator.cs line 116.
            var afterCurrent = _row.IsSimpleValue ? _current + 1 : _current + _row.NumberOfRows;
            var nextValue = afterCurrent + 1;

            if (nextValue < _endCursor)
            {
                _current = nextValue;
                _row = _parent.GetDbRow(nextValue);
                return true;
            }

            _current = _endCursor;
            _hasStarted = false;
            return false;
        }
    }

    /// <summary>
    /// Benchmark-local copy of the row-carrying variant of
    /// SourceResultElement.ArrayEnumerator (ArrayEnumerator.cs lines 20-119) with the same
    /// arrival-decode change as the object enumerator, exposing the cached row to the loop
    /// body as a snapshot (the proposed internal CurrentSnapshot accessor).
    /// </summary>
    private struct RowCarryArrayEnumerator
    {
        private readonly SourceResultDocument _parent;
        private readonly SourceResultDocument.Cursor _containerCursor;
        private readonly SourceResultDocument.Cursor _endCursor;
        private SourceResultDocument.Cursor _current;
        private SourceResultDocument.DbRow _row;
        private bool _hasStarted;

        internal RowCarryArrayEnumerator(
            SourceResultDocument parent,
            SourceResultDocument.Cursor containerCursor,
            SourceResultDocument.DbRow containerRow)
        {
            _parent = parent;
            _containerCursor = containerCursor;
            _endCursor = containerCursor + (containerRow.NumberOfRows - 1);

            _current = default;
            _row = default;
            _hasStarted = false;
        }

        public readonly SourceResultElementSnapshot Current
        {
            get
            {
                if (!_hasStarted)
                {
                    return default;
                }

                return new SourceResultElementSnapshot(_parent, _current, _row);
            }
        }

        public readonly RowCarryArrayEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (!_hasStarted)
            {
                // Identical arithmetic and bounds check as ArrayEnumerator.cs lines 83-99.
                var first = _containerCursor + 1;

                if (first < _endCursor)
                {
                    _current = first;
                    _row = _parent.GetDbRow(first);
                    _hasStarted = true;
                    return true;
                }

                _current = _endCursor;
                _hasStarted = false;
                return false;
            }

            // Cached-row skip replacing the departure GetDbRow at ArrayEnumerator.cs line 105.
            var next = _row.IsSimpleValue ? _current + 1 : _current + _row.NumberOfRows;

            if (next < _endCursor)
            {
                _current = next;
                _row = _parent.GetDbRow(next);
                return true;
            }

            _current = _endCursor;
            _hasStarted = false;
            return false;
        }
    }

    /// <summary>
    /// Benchmark-local stand-in for the grown SourceResultProperty of the recommended design:
    /// it carries the decoded value row (SourceResultProperty.cs lines 10-20 plus a DbRow
    /// field) and is copied per iteration through Current, so the flagged struct-growth cost
    /// is part of the measurement. CreateValueSnapshot mirrors the null-parent behavior of
    /// SourceResultElement.CreateSnapshot (SourceResultElement.cs lines 529-532).
    /// </summary>
    private readonly struct RowCarryProperty
    {
        private readonly SourceResultDocument _parent;
        private readonly SourceResultDocument.Cursor _valueCursor;
        private readonly SourceResultDocument.DbRow _valueRow;

        internal RowCarryProperty(
            SourceResultDocument parent,
            SourceResultDocument.Cursor valueCursor,
            SourceResultDocument.DbRow valueRow)
        {
            _parent = parent;
            _valueCursor = valueCursor;
            _valueRow = valueRow;
        }

        public SourceResultElementSnapshot CreateValueSnapshot()
        {
            if (_parent is null)
            {
                return default;
            }

            return new SourceResultElementSnapshot(_parent, _valueCursor, _valueRow);
        }
    }

    private static byte[] BuildPayload(string shape)
    {
        var payload = new StringBuilder();
        payload.Append("{\"data\":[");

        switch (shape)
        {
            case DenseShape:
                AppendDenseObjects(payload, 1000);
                break;

            case TinyShape:
                for (var i = 0; i < 1000; i++)
                {
                    if (i > 0)
                    {
                        payload.Append(',');
                    }

                    payload.Append("{\"a\":").Append(i).Append('}');
                }

                break;

            case EmptyShape:
                for (var i = 0; i < 1000; i++)
                {
                    if (i > 0)
                    {
                        payload.Append(',');
                    }

                    payload.Append("{}");
                }

                break;

            case SingleShape:
                AppendDenseObjects(payload, 1);
                break;

            default:
                throw new InvalidOperationException($"Unknown shape '{shape}'.");
        }

        payload.Append("]}");
        return Encoding.UTF8.GetBytes(payload.ToString());
    }

    private static void AppendDenseObjects(StringBuilder payload, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                payload.Append(',');
            }

            // 7 mixed-kind properties per object: number, string, number, bool, string
            // array, nested object, null. The nested containers exercise the recursive
            // enumerator construction paths in both variants.
            payload.Append("{\"id\":").Append(i)
                .Append(",\"name\":\"Product ").Append(i).Append('"')
                .Append(",\"price\":").Append(i).Append(".25")
                .Append(",\"inStock\":").Append(i % 2 == 0 ? "true" : "false")
                .Append(",\"tags\":[\"new\",\"sale\",\"eco\"]")
                .Append(",\"dimensions\":{\"width\":").Append(i % 50)
                .Append(",\"height\":").Append(i % 30).Append('}')
                .Append(",\"note\":null}");
        }
    }
}
