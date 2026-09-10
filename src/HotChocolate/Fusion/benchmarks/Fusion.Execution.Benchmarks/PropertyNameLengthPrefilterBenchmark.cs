using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// The payload shape a benchmark invocation scans. Each shape bounds a different
/// scenario of the length prefilter: the win case, the worst-case overhead case,
/// and the tiny-object case.
/// </summary>
public enum PropertyNameShape
{
    /// <summary>
    /// 200 objects with 10 properties of varied name lengths, __typename first,
    /// one escaped name and one container value. The win case: most candidates
    /// are rejected by length before their name bytes are read.
    /// </summary>
    VariedLengths,

    /// <summary>
    /// 200 objects whose 10 property names are all exactly 10 bytes, the length
    /// of __typename. Every candidate passes the prefilter, so this bounds the
    /// overhead at one integer compare per candidate on top of today's work.
    /// </summary>
    UniformLength,

    /// <summary>
    /// 200 objects with a single short property. Bounds the tiny-object case:
    /// one added compare on a hit, one saved name read on a miss.
    /// </summary>
    SingleProperty
}

/// <summary>
/// Measures the backward property-name scan of
/// <c>SourceResultDocument.TryGetNamedPropertyValueCore</c>
/// (SourceResultDocument.TryGetProperty.cs lines 127-210). Today the scan decodes
/// each candidate's name row and then unconditionally calls <c>ReadRawValue</c>
/// (SourceResultDocument.cs lines 384-396 and 402-439), a packed-location decode,
/// segment-table load, bounds branch, span creation and [1..^1] quote slice,
/// before the <c>SequenceEqual</c> at line 197 can reject the candidate on length.
///
/// Names are stored quote-inclusive (AppendStringToken,
/// SourceResultDocument.Parse.cs lines 518-534), so for unescaped names
/// (<c>!row.HasComplexChildren</c>) the row's <c>SizeOrLength</c> equals the
/// unquoted name length plus 2. The optimized variant compares
/// <c>row.SizeOrLength</c> against <c>propertyName.Length + 2</c> on the already
/// loaded row and reads the name bytes only when the lengths agree, which rejects
/// exactly the candidates <c>SequenceEqual</c> would reject on length. Escaped
/// names keep today's unescape path byte for byte.
///
/// Hot callers of this scan: __typename resolution once per abstract-typed
/// element (ValueCompletion.cs line 1291), where the planner injects __typename
/// as the first selection (OperationPlanner.cs lines 3878-3883) so the backward
/// scan visits it last, source path navigation per merged result
/// (FetchResultStore.cs lines 2060, 2116 and 2140-2146), and @interfaceObject
/// upgrade probes (ValueCompletion.cs line 258).
///
/// The product baseline calls the real internal
/// <c>TryGetNamedPropertyValue(Cursor, ReadOnlySpan&lt;byte&gt;, out ...)</c>
/// (SourceResultDocument.TryGetProperty.cs lines 99-125). The core copy is a
/// byte-faithful benchmark-local replica of the same entry and scan; its delta
/// against the product baseline shows replica fidelity, and its delta against
/// the prefilter variant isolates the candidate. Each invocation runs the
/// shape's hit and miss lookups against every object element.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class PropertyNameLengthPrefilterBenchmark
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

    private const int ItemCount = 200;

    // Local copies of HotChocolate.Text.Json JsonConstants.StackallocByteThreshold
    // (JsonConstants.cs line 5) and JsonConstants.BackSlash (line 22). The type is
    // internal to HotChocolate.Text.Json, which grants InternalsVisibleTo to
    // HotChocolate.Fusion.Execution but not to this benchmarks assembly.
    private const int StackallocByteThreshold = 256;
    private const byte BackSlash = (byte)'\\';

    private sealed class Workload
    {
        public SourceResultDocument Document = null!;
        public SourceResultDocument.Cursor[] ObjectCursors = null!;
        public byte[][] LookupNames = null!;
        public bool[] ExpectedFound = null!;
    }

    private MemoryArena _arena = null!;
    private Dictionary<PropertyNameShape, Workload> _workloads = null!;
    private SourceResultDocument _document = null!;
    private SourceResultDocument.Cursor[] _objectCursors = null!;
    private byte[][] _lookupNames = null!;

    public long Consumed;

    [Params(
        PropertyNameShape.VariedLengths,
        PropertyNameShape.UniformLength,
        PropertyNameShape.SingleProperty)]
    public PropertyNameShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _arena = new MemoryArena();
        _workloads = new Dictionary<PropertyNameShape, Workload>();

        foreach (var shape in new[]
        {
            PropertyNameShape.VariedLengths,
            PropertyNameShape.UniformLength,
            PropertyNameShape.SingleProperty
        })
        {
            _workloads[shape] = CreateWorkload(shape);
        }

        // The equivalence check runs over every shape, not only the measured one.
        VerifyEquivalence();

        var current = _workloads[Shape];
        _document = current.Document;
        _objectCursors = current.ObjectCursors;
        _lookupNames = current.LookupNames;

        VerifyChecksums();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var workload in _workloads.Values)
        {
            workload.Document.Dispose();
        }

        _arena.Dispose();
    }

    /// <summary>
    /// Current product behavior: the real internal
    /// <c>SourceResultDocument.TryGetNamedPropertyValue</c> byte overload, which
    /// runs the unmodified private core scan. This entry also pays the
    /// ObjectDisposedException guard the replicas cannot reach (it reads the
    /// private _disposed field); the guard is one branch per lookup.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long TryGetProperty_Product()
    {
        var document = _document;
        var cursors = _objectCursors;
        var lookups = _lookupNames;
        var sum = 0L;

        for (var i = 0; i < cursors.Length; i++)
        {
            for (var n = 0; n < lookups.Length; n++)
            {
                if (document.TryGetNamedPropertyValue(cursors[i], lookups[n], out var value))
                {
                    sum += value._cursor.Value + 1;
                }
            }
        }

        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// Byte-faithful benchmark-local copy of today's entry and core scan. Its delta
    /// against the product baseline shows the replica is equivalent; its delta
    /// against the prefilter variant isolates the candidate on identical code shape.
    /// </summary>
    [Benchmark]
    public long TryGetProperty_CoreCopy()
    {
        var document = _document;
        var cursors = _objectCursors;
        var lookups = _lookupNames;
        var sum = 0L;

        for (var i = 0; i < cursors.Length; i++)
        {
            for (var n = 0; n < lookups.Length; n++)
            {
                if (TryGetPropertyCoreCopy(document, cursors[i], lookups[n], out var value))
                {
                    sum += value._cursor.Value + 1;
                }
            }
        }

        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// Candidate optimization: the same scan with the quoted-length prefilter. For
    /// unescaped candidates the name bytes are read only when
    /// <c>row.SizeOrLength == propertyName.Length + 2</c>; escaped candidates keep
    /// today's path unchanged.
    /// </summary>
    [Benchmark]
    public long TryGetProperty_LengthPrefilter()
    {
        var document = _document;
        var cursors = _objectCursors;
        var lookups = _lookupNames;
        var sum = 0L;

        for (var i = 0; i < cursors.Length; i++)
        {
            for (var n = 0; n < lookups.Length; n++)
            {
                if (TryGetPropertyLengthPrefilter(document, cursors[i], lookups[n], out var value))
                {
                    sum += value._cursor.Value + 1;
                }
            }
        }

        Consumed = sum;
        return sum;
    }

    // Byte-faithful copy of the internal byte overload of
    // SourceResultDocument.TryGetNamedPropertyValue
    // (SourceResultDocument.TryGetProperty.cs lines 99-125). The
    // ObjectDisposedException guard reads the private _disposed field and is
    // omitted; the documents here live for the whole run. CheckExpectedType is
    // private and never fires in this workload (every scanned cursor is a
    // StartObject row), so it is mirrored as a plain guard.
    private static bool TryGetPropertyCoreCopy(
        SourceResultDocument document,
        SourceResultDocument.Cursor objectCursor,
        ReadOnlySpan<byte> propertyName,
        out SourceResultElement value)
    {
        var row = document.GetDbRow(objectCursor);

        if (row.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("The element is not an object.");
        }

        // Only one row means it was EndObject.
        if (row.NumberOfRows == 1)
        {
            value = default;
            return false;
        }

        return TryGetNamedPropertyValueCoreCopy(
            document,
            objectCursor + 1,
            objectCursor + (row.NumberOfRows - 1),
            propertyName,
            out value);
    }

    // Byte-faithful copy of SourceResultDocument.TryGetNamedPropertyValueCore
    // (SourceResultDocument.TryGetProperty.cs lines 127-210). GetDbRow is the
    // internal accessor for the same _parsedData.Get the product calls
    // (SourceResultDocument.Text.cs line 331). The Debug.Asserts of the product
    // are conditional on DEBUG and compile away in the release builds benchmarks
    // run under, so they are omitted here.
    private static bool TryGetNamedPropertyValueCoreCopy(
        SourceResultDocument document,
        SourceResultDocument.Cursor startCursor,
        SourceResultDocument.Cursor endCursor,
        ReadOnlySpan<byte> propertyName,
        out SourceResultElement value)
    {
        Span<byte> utf8UnescapedStack = stackalloc byte[StackallocByteThreshold];

        // Move to the row before the EndObject
        var cursor = endCursor - 1;

        while (cursor > startCursor)
        {
            var row = document.GetDbRow(cursor);

            // Move before the value
            cursor -= row.IsSimpleValue ? 1 : row.NumberOfRows;

            row = document.GetDbRow(cursor);

            var currentPropertyName = ReadUnquotedRawValue(document, row);

            if (row.HasComplexChildren)
            {
                // An escaped property name will be longer than an unescaped candidate,
                // so only unescape when the lengths are compatible.
                if (currentPropertyName.Length > propertyName.Length)
                {
                    var idx = currentPropertyName.IndexOf(BackSlash);

                    // If everything up to where the property name has a backslash matches, keep going.
                    if (propertyName.Length > idx
                        && currentPropertyName[..idx].SequenceEqual(propertyName[..idx]))
                    {
                        var remaining = currentPropertyName.Length - idx;
                        var written = 0;
                        byte[]? rented = null;

                        try
                        {
                            var utf8Unescaped =
                                remaining <= utf8UnescapedStack.Length
                                    ? utf8UnescapedStack
                                    : (rented = ArrayPool<byte>.Shared.Rent(remaining));

                            // Only unescape the part we haven't processed.
                            JsonReaderHelper.Unescape(
                                currentPropertyName[idx..], utf8Unescaped, 0, out written);

                            // If the unescaped remainder matches the input remainder, it's a match.
                            if (utf8Unescaped[..written].SequenceEqual(propertyName[idx..]))
                            {
                                value = new SourceResultElement(document, cursor + 1);
                                return true;
                            }
                        }
                        finally
                        {
                            if (rented is not null)
                            {
                                rented.AsSpan(0, written).Clear();
                                ArrayPool<byte>.Shared.Return(rented);
                            }
                        }
                    }
                }
            }
            else if (currentPropertyName.SequenceEqual(propertyName))
            {
                // If the property name is a match, the answer is the next element.
                value = new SourceResultElement(document, cursor + 1);
                return true;
            }

            // Move to the previous value (name row is at 'id', previous value ends at id - 1)
            cursor -= 1;
        }

        value = default;
        return false;
    }

    // Optimized entry: identical to TryGetPropertyCoreCopy but dispatching into the
    // prefilter core.
    private static bool TryGetPropertyLengthPrefilter(
        SourceResultDocument document,
        SourceResultDocument.Cursor objectCursor,
        ReadOnlySpan<byte> propertyName,
        out SourceResultElement value)
    {
        var row = document.GetDbRow(objectCursor);

        if (row.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("The element is not an object.");
        }

        if (row.NumberOfRows == 1)
        {
            value = default;
            return false;
        }

        return TryGetNamedPropertyValueCorePrefilter(
            document,
            objectCursor + 1,
            objectCursor + (row.NumberOfRows - 1),
            propertyName,
            out value);
    }

    // The recommended design: the loop body branches on HasComplexChildren first,
    // the escaped path is unchanged, and the unescaped path reads the name bytes
    // only when row.SizeOrLength equals the sought name's quoted length. Names are
    // stored quote-inclusive, so for unescaped names SizeOrLength - 2 is the exact
    // unquoted length and the compare rejects exactly what SequenceEqual would
    // reject on length. Everything else is byte-identical to the core copy above.
    private static bool TryGetNamedPropertyValueCorePrefilter(
        SourceResultDocument document,
        SourceResultDocument.Cursor startCursor,
        SourceResultDocument.Cursor endCursor,
        ReadOnlySpan<byte> propertyName,
        out SourceResultElement value)
    {
        Span<byte> utf8UnescapedStack = stackalloc byte[StackallocByteThreshold];

        var targetQuotedLength = propertyName.Length + 2;

        // Move to the row before the EndObject
        var cursor = endCursor - 1;

        while (cursor > startCursor)
        {
            var row = document.GetDbRow(cursor);

            // Move before the value
            cursor -= row.IsSimpleValue ? 1 : row.NumberOfRows;

            row = document.GetDbRow(cursor);

            if (row.HasComplexChildren)
            {
                var currentPropertyName = ReadUnquotedRawValue(document, row);

                // An escaped property name will be longer than an unescaped candidate,
                // so only unescape when the lengths are compatible.
                if (currentPropertyName.Length > propertyName.Length)
                {
                    var idx = currentPropertyName.IndexOf(BackSlash);

                    // If everything up to where the property name has a backslash matches, keep going.
                    if (propertyName.Length > idx
                        && currentPropertyName[..idx].SequenceEqual(propertyName[..idx]))
                    {
                        var remaining = currentPropertyName.Length - idx;
                        var written = 0;
                        byte[]? rented = null;

                        try
                        {
                            var utf8Unescaped =
                                remaining <= utf8UnescapedStack.Length
                                    ? utf8UnescapedStack
                                    : (rented = ArrayPool<byte>.Shared.Rent(remaining));

                            // Only unescape the part we haven't processed.
                            JsonReaderHelper.Unescape(
                                currentPropertyName[idx..], utf8Unescaped, 0, out written);

                            // If the unescaped remainder matches the input remainder, it's a match.
                            if (utf8Unescaped[..written].SequenceEqual(propertyName[idx..]))
                            {
                                value = new SourceResultElement(document, cursor + 1);
                                return true;
                            }
                        }
                        finally
                        {
                            if (rented is not null)
                            {
                                rented.AsSpan(0, written).Clear();
                                ArrayPool<byte>.Shared.Return(rented);
                            }
                        }
                    }
                }
            }
            else if (row.SizeOrLength == targetQuotedLength)
            {
                var currentPropertyName = ReadUnquotedRawValue(document, row);

                if (currentPropertyName.SequenceEqual(propertyName))
                {
                    // If the property name is a match, the answer is the next element.
                    value = new SourceResultElement(document, cursor + 1);
                    return true;
                }
            }

            // Move to the previous value (name row is at 'id', previous value ends at id - 1)
            cursor -= 1;
        }

        value = default;
        return false;
    }

    // Benchmark-local copy of the private SourceResultDocument.ReadRawValue(DbRow, bool)
    // with includeQuotes: false (SourceResultDocument.cs lines 383-396, including its
    // AggressiveInlining). Property names are stored quote-inclusive, so the unquoted
    // name slices one byte in on each side; the chunk read is the internal
    // ReadRawValue(int, int) the private overload delegates to.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> ReadUnquotedRawValue(
        SourceResultDocument document,
        in SourceResultDocument.DbRow row)
    {
        if (row.TokenType is JsonTokenType.String or JsonTokenType.PropertyName)
        {
            return document.ReadRawValue(row.Location, row.SizeOrLength)[1..^1];
        }

        return document.ReadRawValue(row.Location, row.SizeOrLength);
    }

    private Workload CreateWorkload(PropertyNameShape shape)
    {
        var payload = BuildPayload(shape);
        var document = SourceResultDocument.Parse(_arena, payload, payload.Length);

        var products = document.Root
            .GetProperty("data"u8)
            .GetProperty("products"u8);

        var cursors = new SourceResultDocument.Cursor[products.GetArrayLength()];
        var i = 0;

        foreach (var element in products.EnumerateArray())
        {
            cursors[i++] = element._cursor;
        }

        if (i != ItemCount)
        {
            throw new InvalidOperationException(
                $"Shape {shape}: expected {ItemCount} objects but materialized {i}.");
        }

        byte[][] lookupNames;
        bool[] expectedFound;

        switch (shape)
        {
            case PropertyNameShape.VariedLengths:
                // __typename is physically first, so the backward scan visits it last
                // (full scan hit); sku is physically last (first visited hit); escaped
                // resolves through the unescape branch; doesNotExist misses every
                // candidate.
                lookupNames =
                [
                    "__typename"u8.ToArray(),
                    "sku"u8.ToArray(),
                    "escaped"u8.ToArray(),
                    "doesNotExist"u8.ToArray()
                ];
                expectedFound = [true, true, true, false];
                break;

            case PropertyNameShape.UniformLength:
                // Every candidate name is exactly 10 bytes, so both probes pass the
                // prefilter on every candidate: the hit still scans all candidates and
                // the 10-byte miss compares full names on every candidate.
                lookupNames =
                [
                    "__typename"u8.ToArray(),
                    "notpresent"u8.ToArray()
                ];
                expectedFound = [true, false];
                break;

            case PropertyNameShape.SingleProperty:
                lookupNames =
                [
                    "id"u8.ToArray(),
                    "nope"u8.ToArray()
                ];
                expectedFound = [true, false];
                break;

            default:
                throw new InvalidOperationException($"Unknown shape {shape}.");
        }

        return new Workload
        {
            Document = document,
            ObjectCursors = cursors,
            LookupNames = lookupNames,
            ExpectedFound = expectedFound
        };
    }

    private void VerifyEquivalence()
    {
        foreach (var (shape, workload) in _workloads)
        {
            var document = workload.Document;

            for (var i = 0; i < workload.ObjectCursors.Length; i++)
            {
                var cursor = workload.ObjectCursors[i];
                var element = new SourceResultElement(document, cursor);

                // Every present property must be found identically by all three
                // variants; property.Name is the unescaped name, so this also
                // exercises the escaped-name branch of both replicas.
                foreach (var property in element.EnumerateObject())
                {
                    AssertAllVariantsAgree(
                        document,
                        cursor,
                        Encoding.UTF8.GetBytes(property.Name),
                        expectedFound: true,
                        shape,
                        i);
                }

                for (var n = 0; n < workload.LookupNames.Length; n++)
                {
                    AssertAllVariantsAgree(
                        document,
                        cursor,
                        workload.LookupNames[n],
                        workload.ExpectedFound[n],
                        shape,
                        i);
                }
            }
        }
    }

    private static void AssertAllVariantsAgree(
        SourceResultDocument document,
        SourceResultDocument.Cursor objectCursor,
        ReadOnlySpan<byte> propertyName,
        bool expectedFound,
        PropertyNameShape shape,
        int elementIndex)
    {
        var name = Encoding.UTF8.GetString(propertyName);

        var productFound = document.TryGetNamedPropertyValue(
            objectCursor, propertyName, out var productValue);
        var copyFound = TryGetPropertyCoreCopy(
            document, objectCursor, propertyName, out var copyValue);
        var prefilterFound = TryGetPropertyLengthPrefilter(
            document, objectCursor, propertyName, out var prefilterValue);

        if (productFound != expectedFound
            || copyFound != expectedFound
            || prefilterFound != expectedFound)
        {
            throw new InvalidOperationException(
                $"Shape {shape}, element {elementIndex}, name '{name}': expected "
                + $"found={expectedFound} but product={productFound}, "
                + $"copy={copyFound}, prefilter={prefilterFound}.");
        }

        if (expectedFound
            && (!ReferenceEquals(productValue._parent, copyValue._parent)
                || !ReferenceEquals(productValue._parent, prefilterValue._parent)
                || productValue._cursor != copyValue._cursor
                || productValue._cursor != prefilterValue._cursor))
        {
            throw new InvalidOperationException(
                $"Shape {shape}, element {elementIndex}, name '{name}': the variants "
                + $"resolved different elements (product {productValue._cursor}, "
                + $"copy {copyValue._cursor}, prefilter {prefilterValue._cursor}).");
        }
    }

    private void VerifyChecksums()
    {
        var productSum = TryGetProperty_Product();
        var copySum = TryGetProperty_CoreCopy();
        var prefilterSum = TryGetProperty_LengthPrefilter();

        if (productSum != copySum || productSum != prefilterSum)
        {
            throw new InvalidOperationException(
                $"Checksum mismatch for shape {Shape}: product {productSum}, "
                + $"copy {copySum}, prefilter {prefilterSum}.");
        }
    }

    private static byte[] BuildPayload(PropertyNameShape shape)
    {
        // Every payload stays far below the 128 KiB single-chunk limit, so all name
        // reads take the single-chunk fast path of ReadRawValue and the cross-chunk
        // copy path cannot skew the comparison.
        var json = new StringBuilder(64 * 1024);
        json.Append("{\"data\":{\"products\":[");

        for (var i = 0; i < ItemCount; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            switch (shape)
            {
                case PropertyNameShape.VariedLengths:
                    // Name byte lengths: 10, 2, 4, 5, 7, 6, 4, 11, 12 raw (7 unescaped), 3.
                    // Only __typename is 10 bytes, so a __typename probe length-rejects
                    // every other unescaped candidate. "tags" holds a container value so
                    // the scan's NumberOfRows skip runs; "escaped" parses with
                    // ValueIsEscaped and unescapes to "escaped".
                    json.Append("{\"__typename\":\"Product\"");
                    json.Append(",\"id\":\"prod-").Append(i).Append('"');
                    json.Append(",\"name\":\"Product ").Append(i).Append('"');
                    json.Append(",\"price\":").Append(i).Append(".99");
                    json.Append(",\"inStock\":").Append(i % 3 == 0 ? "false" : "true");
                    json.Append(",\"rating\":4.5");
                    json.Append(",\"tags\":[\"red\",\"new\"]");
                    json.Append(",\"description\":\"Plain description for item ").Append(i).Append('"');
                    json.Append(",\"esc\\u0061ped\":\"value-").Append(i).Append('"');
                    json.Append(",\"sku\":\"SKU-").Append(i).Append("\"}");
                    break;

                case PropertyNameShape.UniformLength:
                    // All ten names are exactly 10 bytes, matching the __typename probe.
                    json.Append("{\"__typename\":\"Product\"");
                    json.Append(",\"identifier\":\"prod-").Append(i).Append('"');
                    json.Append(",\"namefield0\":\"Product ").Append(i).Append('"');
                    json.Append(",\"pricefield\":").Append(i).Append(".99");
                    json.Append(",\"inventory0\":").Append(i % 3 == 0 ? "false" : "true");
                    json.Append(",\"quantity00\":").Append(i % 50);
                    json.Append(",\"ratingval0\":4.5");
                    json.Append(",\"descriptn0\":\"Plain description ").Append(i).Append('"');
                    json.Append(",\"extrafeld0\":\"extra-").Append(i).Append('"');
                    json.Append(",\"skunumber0\":\"SKU-").Append(i).Append("\"}");
                    break;

                case PropertyNameShape.SingleProperty:
                    json.Append("{\"id\":\"prod-").Append(i).Append("\"}");
                    break;

                default:
                    throw new InvalidOperationException($"Unknown shape {shape}.");
            }
        }

        json.Append("]}}");
        return Encoding.UTF8.GetBytes(json.ToString());
    }
}
