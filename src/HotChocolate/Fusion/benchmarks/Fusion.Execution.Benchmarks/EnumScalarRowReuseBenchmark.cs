using System;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Isolates the MetaDb row-decode cost of enum and scalar list-element completion on the
/// FetchResultStore.SaveSafeResult -> ValueCompletion.BuildResult -> TryCompleteList hot path.
///
/// The baseline replays the current per-element decode sequence
/// (ValueCompletion.cs line 979 element dispatch, lines 1058-1079 CompleteEnumValue, plus the
/// SetLeafValue-internal source.GetValueRow() at CompositeResultDocument.cs lines 514-516):
/// an enum-typed element decodes the same source row four times (ValueKind for the list
/// dispatch, ValueKind again inside CompleteEnumValue, ValueSpan, and GetValueRow inside
/// SetLeafValue) and a scalar element decodes it twice (ValueKind, then GetValueRow inside
/// SetLeafValue).
///
/// The row-reuse variant decodes the row once via GetValueRow and derives the token type and
/// the raw value bytes from that row, standing in for passing the row through CompleteEnumValue
/// and into the existing SetLeafValue(source, row) overload (CompositeResultElement.cs
/// line 1045). The enum-membership check (FusionEnumValueCollection.ContainsName) runs
/// identically in both variants, so the variants differ only in row decode counts. The
/// selection.NamedType type test of CompleteEnumValue is factored out of both variants because
/// it never touches the source row.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class EnumScalarRowReuseBenchmark
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

    private const int ElementCount = 1000;

    private static readonly string[] s_memberNames = ["RED", "GREEN", "BLUE"];

#pragma warning disable IDE0370 // Remove unnecessary suppression
    private MemoryArena _arena = null!;
    private SourceResultDocument _document = null!;
    private SourceResultElement[] _enumElements = null!;
    private SourceResultElement[] _numberElements = null!;
    private FusionEnumValueCollection _enumValues = null!;
#pragma warning restore IDE0370 // Remove unnecessary suppression

    [GlobalSetup]
    public void Setup()
    {
        _arena = new MemoryArena();

        var payload = new StringBuilder();
        payload.Append("{\"data\":{\"colors\":[");

        for (var i = 0; i < ElementCount; i++)
        {
            if (i > 0)
            {
                payload.Append(',');
            }

            // Every 10th value is not a member of the composite enum so the masking
            // branch of CompleteEnumValue (SetNullValue, no further row decode) is
            // exercised as well.
            var name = i % 10 == 9 ? "PURPLE" : s_memberNames[i % 3];
            payload.Append('"').Append(name).Append('"');
        }

        payload.Append("],\"numbers\":[");

        for (var i = 0; i < ElementCount; i++)
        {
            if (i > 0)
            {
                payload.Append(',');
            }

            // 1 to 5 digit numbers so value lengths vary.
            payload.Append(i * 37);
        }

        payload.Append("]}}");

        // The payload is far below the 128 KiB single-chunk limit, so the document is
        // parsed in place over one chunk and every ValueSpan read stays allocation-free.
        var json = Encoding.UTF8.GetBytes(payload.ToString());
        _document = SourceResultDocument.Parse(_arena, json, json.Length);

        var data = _document.Root.GetProperty("data"u8);

        _enumElements = MaterializeArrayElements(data.GetProperty("colors"u8));
        _numberElements = MaterializeArrayElements(data.GetProperty("numbers"u8));

        _enumValues = new FusionEnumValueCollection(
        [
            new FusionEnumValue("RED", null, false, null, false),
            new FusionEnumValue("GREEN", null, false, null, false),
            new FusionEnumValue("BLUE", null, false, null, false)
        ]);

        VerifyVariantsProduceIdenticalResults();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _document.Dispose();
        _arena.Dispose();
    }

    /// <summary>
    /// Current product decode sequence, using only real product internals. Per enum element
    /// the source row is decoded four times, per number element twice. GetValueRow stands in
    /// for target.SetLeafValue(source), whose AssignSourceValue overload performs exactly
    /// that decode (CompositeResultDocument.cs lines 514-516) and then consumes the row's
    /// TokenType, Location and SizeOrLength, which the checksum consumes here.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long CurrentDecodeSequence()
    {
        var checksum = 0L;
        var enumElements = _enumElements;
        var enumValues = _enumValues;

        for (var i = 0; i < enumElements.Length; i++)
        {
            var element = enumElements[i];

            // TryCompleteList element dispatch, ValueCompletion.cs line 979: first row decode.
            var elementValueKind = element.ValueKind;

            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                checksum--;
                continue;
            }

            // CompleteEnumValue, ValueCompletion.cs lines 1069-1078: the ValueKind re-check
            // is the second row decode and ValueSpan is the third (full MetaDb Get plus
            // ReadRawValue).
            if (element.ValueKind is JsonValueKind.String)
            {
                var valueSpan = element.ValueSpan;

                if (enumValues.ContainsName(valueSpan))
                {
                    // target.SetLeafValue(source), ValueCompletion.cs line 1073, performs the
                    // fourth decode through AssignSourceValue -> source.GetValueRow().
                    var row = element.GetValueRow();
                    checksum += (int)row.TokenType + row.Location + row.SizeOrLength
                        + valueSpan.Length + (int)elementValueKind;
                }
                else
                {
                    // target.SetNullValue() does not decode the source row.
                    checksum--;
                }
            }
            else
            {
                checksum--;
            }
        }

        var numberElements = _numberElements;

        for (var i = 0; i < numberElements.Length; i++)
        {
            var element = numberElements[i];

            // TryCompleteList element dispatch, ValueCompletion.cs line 979: first row decode.
            var elementValueKind = element.ValueKind;

            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                checksum--;
                continue;
            }

            // Scalar case, ValueCompletion.cs line 1008: targetElement.SetLeafValue(element)
            // performs the second decode through AssignSourceValue -> source.GetValueRow().
            var row = element.GetValueRow();
            checksum += (int)row.TokenType + row.Location + row.SizeOrLength
                + (int)elementValueKind;
        }

        return checksum;
    }

    /// <summary>
    /// Candidate optimization: one GetValueRow per element; the token type and the raw value
    /// bytes derive from that row without touching the MetaDb again. Production shape: pass
    /// the row into CompleteEnumValue and the existing SetLeafValue(source, row) overload.
    /// </summary>
    [Benchmark]
    public long RowReuseSingleDecode()
    {
        var checksum = 0L;
        var document = _document;
        var enumElements = _enumElements;
        var enumValues = _enumValues;

        for (var i = 0; i < enumElements.Length; i++)
        {
            var element = enumElements[i];

            // Single row decode; everything below derives from this row.
            var row = element.GetValueRow();
            var elementValueKind = row.TokenType.ToValueKind();

            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                checksum--;
                continue;
            }

            if (elementValueKind is JsonValueKind.String)
            {
                var valueSpan = ReadUnquotedRawValue(document, row);

                if (enumValues.ContainsName(valueSpan))
                {
                    // The row is already in hand, so SetLeafValue(source, row)
                    // (CompositeResultElement.cs line 1045) needs no further decode.
                    checksum += (int)row.TokenType + row.Location + row.SizeOrLength
                        + valueSpan.Length + (int)elementValueKind;
                }
                else
                {
                    checksum--;
                }
            }
            else
            {
                checksum--;
            }
        }

        var numberElements = _numberElements;

        for (var i = 0; i < numberElements.Length; i++)
        {
            var element = numberElements[i];

            // Single row decode replacing both ValueKind and the SetLeafValue-internal read.
            var row = element.GetValueRow();
            var elementValueKind = row.TokenType.ToValueKind();

            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                checksum--;
                continue;
            }

            checksum += (int)row.TokenType + row.Location + row.SizeOrLength
                + (int)elementValueKind;
        }

        return checksum;
    }

    // Benchmark-local copy of the private SourceResultDocument.ReadRawValue(DbRow, bool) with
    // includeQuotes: false, mirrored from
    // src/HotChocolate/Fusion/src/Fusion.Execution/Text/Json/SourceResultDocument.cs
    // lines 365-378. Strings are stored quote-inclusive, so the unquoted value slices one byte
    // in on each side; the chunk read is the internal ReadRawValue(int, int) that the private
    // overload delegates to, so the returned bytes are identical to ValueSpan.
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

    private static SourceResultElement[] MaterializeArrayElements(SourceResultElement array)
    {
        var elements = new SourceResultElement[array.GetArrayLength()];
        var i = 0;

        foreach (var element in array.EnumerateArray())
        {
            elements[i++] = element;
        }

        if (i != elements.Length)
        {
            throw new InvalidOperationException("Array materialization is incomplete.");
        }

        return elements;
    }

    private void VerifyVariantsProduceIdenticalResults()
    {
        var document = _document;

        for (var i = 0; i < _enumElements.Length; i++)
        {
            var element = _enumElements[i];

            var baselineKind = element.ValueKind;
            var baselineSpan = element.ValueSpan;
            var baselineRow = element.GetValueRow();
            var baselineIsMember = _enumValues.ContainsName(baselineSpan);

            var row = element.GetValueRow();
            var optimizedKind = row.TokenType.ToValueKind();
            var optimizedSpan = ReadUnquotedRawValue(document, row);
            var optimizedIsMember = _enumValues.ContainsName(optimizedSpan);

            if (baselineKind != optimizedKind
                || !baselineSpan.SequenceEqual(optimizedSpan)
                || baselineIsMember != optimizedIsMember
                || baselineRow.TokenType != row.TokenType
                || baselineRow.Location != row.Location
                || baselineRow.SizeOrLength != row.SizeOrLength)
            {
                throw new InvalidOperationException(
                    $"Enum element {i} decodes differently between the baseline and the "
                    + "row-reuse variant.");
            }
        }

        for (var i = 0; i < _numberElements.Length; i++)
        {
            var element = _numberElements[i];

            var baselineKind = element.ValueKind;
            var baselineRow = element.GetValueRow();

            var row = element.GetValueRow();
            var optimizedKind = row.TokenType.ToValueKind();

            if (baselineKind != optimizedKind
                || baselineRow.TokenType != row.TokenType
                || baselineRow.Location != row.Location
                || baselineRow.SizeOrLength != row.SizeOrLength)
            {
                throw new InvalidOperationException(
                    $"Number element {i} decodes differently between the baseline and the "
                    + "row-reuse variant.");
            }
        }

        var baselineChecksum = CurrentDecodeSequence();
        var optimizedChecksum = RowReuseSingleDecode();

        if (baselineChecksum != optimizedChecksum)
        {
            throw new InvalidOperationException(
                $"Checksum mismatch: baseline {baselineChecksum} vs "
                + $"row-reuse {optimizedChecksum}.");
        }
    }
}
