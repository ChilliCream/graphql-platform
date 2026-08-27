using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Proof-obligation benchmark for the wide condition-mask change: the narrow include
/// check must stay free. <c>IsIncluded_Baseline</c> drives a byte-faithful copy of
/// the pre-change <c>Selection.IsIncluded</c> body (Execution/Nodes/Selection.cs
/// line 257 at the pre-change commit) over the very same compiled <c>Selection</c>
/// instances the product kernel reads, accessing the private <c>_includeFlags</c>
/// field through an <see cref="UnsafeAccessorAttribute"/> (which the JIT lowers to
/// the same field load); <c>IsIncluded_Product</c> drives the product
/// <c>Selection.IsIncludedUnchecked</c> (Execution/Nodes/Selection.cs line 289),
/// which is textually that body. The ratio must be 1.00 within noise with zero
/// allocations. <c>IsIncluded_BaselineCopy</c> is the layout-noise control (baseline
/// copy vs itself pattern), and <c>IsIncluded_Wide</c> is an informational row for
/// <c>Selection.IsIncludedWide</c> (Execution/Nodes/Selection.cs line 339) on an
/// operation with more than 64 include conditions (no gate).
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class WideConditionMaskBenchmark
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

    private const int SelectionCount = 1000;

    [Params(48)]
    public int PathCount = 48;

    private Selection[] _selections = null!;
    private Selection[] _wideSelections = null!;
    private ulong[] _requestFlags = null!;
    private ConditionFlags[] _conditionFlags = null!;
    private ulong[] _wideRequestFlags = null!;

    [GlobalSetup]
    public void Setup()
    {
        var schema = CreateSchema();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        var narrowOperation = compiler.Compile(
            "1", "1", "1", ParseOperation(CreateDocument(PathCount, padToWide: false)));
        var wideOperation = compiler.Compile(
            "2", "2", "2", ParseOperation(CreateDocument(PathCount, padToWide: true)));

        if (narrowOperation.HasWideIncludeFlags || !wideOperation.HasWideIncludeFlags)
        {
            throw new InvalidOperationException("The compiled operation widths are wrong.");
        }

        _selections = CollectSelections(narrowOperation);
        _wideSelections = CollectSelections(wideOperation);

        // Alternate between a mask that satisfies only the last path (the loop scans
        // all paths and hits on the last one) and a mask that satisfies none.
        var lastPathBit = 1ul << (PathCount - 1);
        _requestFlags = new ulong[SelectionCount];
        _conditionFlags = new ConditionFlags[SelectionCount];

        for (var i = 0; i < SelectionCount; i++)
        {
            _requestFlags[i] = (i & 1) == 0 ? lastPathBit : 0ul;
            _conditionFlags[i] = new ConditionFlags(_requestFlags[i]);
        }

        // One overflow word; the measured conditions all sit in word 0.
        _wideRequestFlags = new ulong[1];

        VerifyEquivalence();
    }

    /// <summary>
    /// Every kernel must agree on every selection for every request mask up front,
    /// so a divergence fails the run instead of producing a wrong comparison.
    /// </summary>
    private void VerifyEquivalence()
    {
        ReadOnlySpan<ulong> testMasks = [0ul, 1ul, 1ul << (PathCount - 1), ulong.MaxValue];

        foreach (var mask in testMasks)
        {
            for (var i = 0; i < SelectionCount; i++)
            {
                var baseline = IsIncludedBaseline(_selections[i], mask);
                var product = _selections[i].IsIncludedUnchecked(mask);
                var conditionFlags = _selections[i].IsIncluded(new ConditionFlags(mask));
                var spanPair = _selections[i].IsIncluded(mask, []);
#pragma warning disable CS0618
                var raw = _selections[i].IsIncluded(mask);
#pragma warning restore CS0618
                var wide = _wideSelections[i].IsIncludedWide(mask, _wideRequestFlags);

                if (baseline != product
                    || baseline != conditionFlags
                    || baseline != spanPair
                    || baseline != raw
                    || baseline != wide)
                {
                    throw new InvalidOperationException(
                        $"Kernel divergence for mask {mask} at selection {i}.");
                }
            }
        }
    }

    [Benchmark(Baseline = true)]
    public int IsIncluded_Baseline()
    {
        var count = 0;
        var selections = _selections;
        var requestFlags = _requestFlags;

        for (var i = 0; i < selections.Length; i++)
        {
            if (IsIncludedBaseline(selections[i], requestFlags[i]))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int IsIncluded_Product()
    {
        var count = 0;
        var selections = _selections;
        var requestFlags = _requestFlags;

        for (var i = 0; i < selections.Length; i++)
        {
            if (selections[i].IsIncludedUnchecked(requestFlags[i]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Layout-noise control: drives the baseline kernel a second time through its own
    /// method body. Any Ratio deviation of this row from 1.00 bounds the code-layout
    /// noise floor the product row must be judged against.
    /// </summary>
    [Benchmark]
    public int IsIncluded_BaselineCopy()
    {
        var count = 0;
        var selections = _selections;
        var requestFlags = _requestFlags;

        for (var i = 0; i < selections.Length; i++)
        {
            if (IsIncludedBaselineCopy(selections[i], requestFlags[i]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Carrier comparison for a narrow operation with 48 include conditions. This
    /// is the public condition-flags API used by request execution.
    /// </summary>
    [Benchmark]
    public int IsIncluded_ConditionFlags()
    {
        var count = 0;
        var selections = _selections;
        var conditionFlags = _conditionFlags;

        for (var i = 0; i < selections.Length; i++)
        {
            if (selections[i].IsIncluded(conditionFlags[i]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Carrier comparison for the internal word-0 and overflow-span pair used by
    /// the narrow execution hot path.
    /// </summary>
    [Benchmark]
    public int IsIncluded_SpanPair()
    {
        var count = 0;
        var selections = _selections;
        var requestFlags = _requestFlags;

        for (var i = 0; i < selections.Length; i++)
        {
            if (selections[i].IsIncluded(requestFlags[i], []))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Carrier comparison for the deprecated narrow-only raw-word API.
    /// </summary>
    [Benchmark]
    public int IsIncluded_Ulong()
    {
        var count = 0;
        var selections = _selections;
        var requestFlags = _requestFlags;

        for (var i = 0; i < selections.Length; i++)
        {
#pragma warning disable CS0618
            if (selections[i].IsIncluded(requestFlags[i]))
#pragma warning restore CS0618
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Informational: the wide path on selections of an operation with more than 64
    /// include conditions. No gate.
    /// </summary>
    [Benchmark]
    public int IsIncluded_Wide()
    {
        var count = 0;
        var selections = _wideSelections;
        var requestFlags = _requestFlags;
        var wideRequestFlags = _wideRequestFlags;

        for (var i = 0; i < selections.Length; i++)
        {
            if (selections[i].IsIncludedWide(requestFlags[i], wideRequestFlags))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Direct access to the private <c>Selection._includeFlags</c> field. The JIT
    /// lowers the accessor to the same field load the product method performs, so
    /// the baseline copy reads the compiled selections exactly like the product.
    /// </summary>
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_includeFlags")]
    private static extern ref ulong[] IncludeFlagsOf(Selection selection);

    /// <summary>
    /// Byte-faithful copy of the pre-change <c>Selection.IsIncluded(ulong)</c> body,
    /// with each <c>_includeFlags</c> mention replaced by the field accessor.
    /// </summary>
    private static bool IsIncludedBaseline(Selection selection, ulong includeFlags)
    {
        if (IncludeFlagsOf(selection).Length == 0)
        {
            return true;
        }

        if (IncludeFlagsOf(selection).Length == 1)
        {
            var flags1 = IncludeFlagsOf(selection)[0];
            return (flags1 & includeFlags) == flags1;
        }

        if (IncludeFlagsOf(selection).Length == 2)
        {
            var flags1 = IncludeFlagsOf(selection)[0];
            var flags2 = IncludeFlagsOf(selection)[1];
            return (flags1 & includeFlags) == flags1 || (flags2 & includeFlags) == flags2;
        }

        if (IncludeFlagsOf(selection).Length == 3)
        {
            var flags1 = IncludeFlagsOf(selection)[0];
            var flags2 = IncludeFlagsOf(selection)[1];
            var flags3 = IncludeFlagsOf(selection)[2];
            return (flags1 & includeFlags) == flags1
                || (flags2 & includeFlags) == flags2
                || (flags3 & includeFlags) == flags3;
        }

        var includeFlagsArray = IncludeFlagsOf(selection);

        for (var i = 0; i < includeFlagsArray.Length; i++)
        {
            var current = includeFlagsArray[i];

            if ((current & includeFlags) == current)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Exact textual copy of <see cref="IsIncludedBaseline"/> for the layout-noise control.
    /// </summary>
    private static bool IsIncludedBaselineCopy(Selection selection, ulong includeFlags)
    {
        if (IncludeFlagsOf(selection).Length == 0)
        {
            return true;
        }

        if (IncludeFlagsOf(selection).Length == 1)
        {
            var flags1 = IncludeFlagsOf(selection)[0];
            return (flags1 & includeFlags) == flags1;
        }

        if (IncludeFlagsOf(selection).Length == 2)
        {
            var flags1 = IncludeFlagsOf(selection)[0];
            var flags2 = IncludeFlagsOf(selection)[1];
            return (flags1 & includeFlags) == flags1 || (flags2 & includeFlags) == flags2;
        }

        if (IncludeFlagsOf(selection).Length == 3)
        {
            var flags1 = IncludeFlagsOf(selection)[0];
            var flags2 = IncludeFlagsOf(selection)[1];
            var flags3 = IncludeFlagsOf(selection)[2];
            return (flags1 & includeFlags) == flags1
                || (flags2 & includeFlags) == flags2
                || (flags3 & includeFlags) == flags3;
        }

        var includeFlagsArray = IncludeFlagsOf(selection);

        for (var i = 0; i < includeFlagsArray.Length; i++)
        {
            var current = includeFlagsArray[i];

            if ((current & includeFlags) == current)
            {
                return true;
            }
        }

        return false;
    }

    private static Selection[] CollectSelections(Operation operation)
    {
        var selections = new Selection[SelectionCount];

        for (var i = 0; i < SelectionCount; i++)
        {
            if (!operation.RootSelectionSet.TryGetSelection($"a{i}", out var selection))
            {
                throw new InvalidOperationException($"The selection a{i} is missing.");
            }

            selections[i] = selection;
        }

        return selections;
    }

    /// <summary>
    /// Builds an operation whose selections a0..a999 each carry
    /// <see cref="PathCount"/> conditioned paths: one inline fragment per path
    /// variable, each containing all aliased fields. With <paramref name="padToWide"/>,
    /// 65 extra conditional fields push the include-condition count over 64 so the
    /// operation compiles wide (the measured conditions stay in word 0).
    /// </summary>
    private string CreateDocument(int pathCount, bool padToWide)
    {
        var sourceText = new StringBuilder();
        sourceText.Append("query(");

        for (var p = 0; p < pathCount; p++)
        {
            sourceText.Append($"$v{p}: Boolean! ");
        }

        if (padToWide)
        {
            for (var i = 0; i < 65; i++)
            {
                sourceText.Append($"$w{i}: Boolean! ");
            }
        }

        sourceText.Append(") {");

        for (var p = 0; p < pathCount; p++)
        {
            sourceText.Append($" ... @include(if: $v{p}) {{");

            for (var i = 0; i < SelectionCount; i++)
            {
                sourceText.Append($" a{i}: foo");
            }

            sourceText.Append(" }");
        }

        if (padToWide)
        {
            for (var i = 0; i < 65; i++)
            {
                sourceText.Append($" p{i}: foo @include(if: $w{i})");
            }
        }

        sourceText.Append(" }");
        return sourceText.ToString();
    }

    private static OperationDefinitionNode ParseOperation(string sourceText)
        // The generated documents exceed the default 2048-field parser cap.
        => Utf8GraphQLParser.Parse(sourceText, new ParserOptions(maxAllowedFields: int.MaxValue))
            .Definitions
            .OfType<OperationDefinitionNode>()
            .First();

    private static FusionSchemaDefinition CreateSchema()
    {
        var sourceSchemas = new List<SourceSchemaText>
        {
            new(
                "a",
                """
                type Query {
                  foo: String
                }
                """)
        };

        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(
            sourceSchemas,
            new SchemaComposerOptions(),
            compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

}
