using System;
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
/// Measures the per-result source-path walk of <c>FetchResultStore.GetDataElement</c>
/// (FetchResultStore.cs lines 1969-2023). Today every field segment calls the string
/// overload of <c>SourceResultElement.TryGetProperty</c>, which lands in
/// <c>SourceResultDocument.TryGetNamedPropertyValue(ReadOnlySpan&lt;char&gt;)</c>
/// (SourceResultDocument.TryGetProperty.cs lines 10-97): per call it pays
/// <c>Encoding.GetMaxByteCount</c>, a zeroed 256-byte stackalloc (no [SkipLocalsInit]),
/// and <c>Encoding.GetBytes</c> before the UTF-8 name scan. In the batch merge loops
/// (FetchResultStore.cs lines 127-140 and 239-242) the same plan-static
/// <c>sourcePath</c> is re-walked once per result, so the identical transcode repeats
/// N results times path depth per merge.
///
/// The candidate transcodes each segment name to UTF-8 once per merge and walks every
/// result with the <c>ReadOnlySpan&lt;byte&gt;</c> overload. Both overload families
/// converge on <c>TryGetNamedPropertyValueCore</c>, so the resolved elements are
/// identical; the byte overload skips only the per-call transcode preamble (the Core
/// scan itself still zeroes its own 256-byte unescape buffer in both variants).
///
/// The workload parses 500 independent <see cref="SourceResultDocument"/> instances,
/// one per simulated <c>SourceSchemaResult</c>, rather than one document with a
/// 500-element array: in the real batch loop each result carries its own document and
/// GetDataElement starts at that document's own root, so per-result documents
/// reproduce the real walk (root object row scan per document) exactly. Each document
/// is shaped like {"data":{"productById":{...,"reviews":[...]}}} and the walked path
/// is data -&gt; productById -&gt; reviews (3 field segments).
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class SourcePathTranscodeHoistBenchmark
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

    private const int ResultCount = 500;
    private const int SegmentCount = 3;

    // data(4) + productById(11) + reviews(7) = 22 UTF-8 bytes; 64 leaves headroom
    // and stays a trivial stack cost. Setup verifies the names actually fit.
    private const int Utf8BufferSize = 64;

    // The plan-static source path GetDataElement walks per result. In the product
    // these are SelectionPathSegment.Name values of Field segments.
    private static readonly string[] s_segmentNames = ["data", "productById", "reviews"];

    private MemoryArena _arena = null!;
    private SourceResultDocument[] _documents = null!;

    public long Consumed;

    [GlobalSetup]
    public void Setup()
    {
        _arena = new MemoryArena();
        _documents = new SourceResultDocument[ResultCount];

        for (var i = 0; i < ResultCount; i++)
        {
            var payload = BuildPayload(i);
            _documents[i] = SourceResultDocument.Parse(_arena, payload, payload.Length);
        }

        VerifyEquivalence();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var document in _documents)
        {
            document.Dispose();
        }

        _arena.Dispose();
    }

    /// <summary>
    /// Current product behavior: per result, every path segment goes through the
    /// string overload of TryGetProperty, exactly like GetDataElement line 1993,
    /// paying the transcode preamble N results x depth times per merge.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long Baseline_StringPerResult()
    {
        var documents = _documents;
        var segmentNames = s_segmentNames;
        var sum = 0L;

        for (var i = 0; i < documents.Length; i++)
        {
            var element = WalkString(documents[i].Root, segmentNames);
            sum += element.GetArrayLength();
        }

        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// Candidate: the segment names are transcoded to UTF-8 once per merge into a
    /// stack buffer (the product would use a stack or pooled buffer at the top of
    /// AddPartialResults), then every result is walked with the real
    /// ReadOnlySpan&lt;byte&gt; overload of TryGetProperty.
    /// </summary>
    [Benchmark]
    public long Hoisted_Utf8OncePerMerge()
    {
        var documents = _documents;
        var segmentNames = s_segmentNames;

        // Once per merge: transcode each segment name, recording exclusive end
        // offsets so segment s spans utf8Names[ends[s - 1]..ends[s]].
        Span<byte> utf8Names = stackalloc byte[Utf8BufferSize];
        Span<int> ends = stackalloc int[SegmentCount];
        var offset = 0;

        for (var s = 0; s < segmentNames.Length; s++)
        {
            offset += Encoding.UTF8.GetBytes(segmentNames[s], utf8Names[offset..]);
            ends[s] = offset;
        }

        var sum = 0L;

        for (var i = 0; i < documents.Length; i++)
        {
            var element = WalkUtf8(documents[i].Root, utf8Names, ends);
            sum += element.GetArrayLength();
        }

        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// Byte-faithful copy of the field-segment loop of GetDataElement
    /// (FetchResultStore.cs lines 1976-1998 and 2022): the not-an-object guard with
    /// null propagation, then TryGetProperty with the segment name string. The
    /// InlineFragment branch is out of scope here; the walked path has only field
    /// segments, matching the common entity fan-out source path.
    /// </summary>
    private static SourceResultElement WalkString(SourceResultElement data, string[] segmentNames)
    {
        var current = data;

        for (var i = 0; i < segmentNames.Length; i++)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return current.ValueKind is JsonValueKind.Null ? current : default;
            }

            if (!current.TryGetProperty(segmentNames[i], out current))
            {
                return default;
            }
        }

        return current;
    }

    /// <summary>
    /// Identical walk with the pre-transcoded names: the only difference is that
    /// TryGetProperty receives ReadOnlySpan&lt;byte&gt;, entering
    /// TryGetNamedPropertyValueCore directly without the per-call transcode.
    /// </summary>
    private static SourceResultElement WalkUtf8(
        SourceResultElement data,
        ReadOnlySpan<byte> utf8Names,
        ReadOnlySpan<int> ends)
    {
        var current = data;
        var start = 0;

        for (var i = 0; i < ends.Length; i++)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return current.ValueKind is JsonValueKind.Null ? current : default;
            }

            if (!current.TryGetProperty(utf8Names[start..ends[i]], out current))
            {
                return default;
            }

            start = ends[i];
        }

        return current;
    }

    private void VerifyEquivalence()
    {
        var utf8Names = new byte[Utf8BufferSize];
        var ends = new int[SegmentCount];
        var offset = 0;

        for (var s = 0; s < s_segmentNames.Length; s++)
        {
            var count = Encoding.UTF8.GetByteCount(s_segmentNames[s]);

            if (offset + count > Utf8BufferSize)
            {
                throw new InvalidOperationException(
                    "The segment names do not fit the transcode buffer; "
                    + "increase Utf8BufferSize.");
            }

            offset += Encoding.UTF8.GetBytes(s_segmentNames[s], utf8Names.AsSpan(offset));
            ends[s] = offset;
        }

        for (var i = 0; i < _documents.Length; i++)
        {
            var baseline = WalkString(_documents[i].Root, s_segmentNames);
            var hoisted = WalkUtf8(_documents[i].Root, utf8Names, ends);

            // Same value identity: same parent document instance and same MetaDb
            // cursor mean both walks resolved the exact same row.
            if (!ReferenceEquals(baseline._parent, hoisted._parent)
                || !baseline._cursor.Equals(hoisted._cursor))
            {
                throw new InvalidOperationException(
                    $"Result {i}: baseline and hoisted walks resolved different elements.");
            }

            if (baseline.ValueKind != JsonValueKind.Array || baseline.GetArrayLength() < 1)
            {
                throw new InvalidOperationException(
                    $"Result {i}: the walk did not land on the non-empty reviews array "
                    + "(the workload would silently measure the miss path).");
            }
        }
    }

    private static byte[] BuildPayload(int index)
    {
        // Sibling properties before and after the walked segments keep the reverse
        // linear property scan of TryGetNamedPropertyValueCore realistic.
        var reviewCount = 1 + (index % 5);
        var json = new StringBuilder(512);

        json.Append("{\"data\":{\"productById\":{");
        json.Append("\"id\":\"product-").Append(index).Append("\",");
        json.Append("\"name\":\"Product ").Append(index).Append("\",");
        json.Append("\"reviews\":[");

        for (var i = 0; i < reviewCount; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            json.Append("{\"id\":\"review-").Append(index).Append('-').Append(i).Append("\",");
            json.Append("\"body\":\"Review body ").Append(i).Append("\",");
            json.Append("\"stars\":").Append(1 + (i % 5)).Append('}');
        }

        json.Append("],");
        json.Append("\"sku\":\"SKU-").Append(index).Append("\"}}}");

        return Encoding.UTF8.GetBytes(json.ToString());
    }
}
