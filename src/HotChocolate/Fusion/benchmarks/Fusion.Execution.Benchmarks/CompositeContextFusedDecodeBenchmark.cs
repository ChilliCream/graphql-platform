using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
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
using CompositeCursor = HotChocolate.Fusion.Text.Json.CompositeResultDocument.Cursor;
using OperationReferenceType = HotChocolate.Fusion.Text.Json.CompositeResultDocument.OperationReferenceType;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures the redundant MetaDb row decodes ValueCompletion pays to classify an
/// already-initialized merge target and then acquire its object or array context.
///
/// Object path: BuildResult's already-initialized branch (ValueCompletion.cs lines 82-90)
/// reads target.ValueKind (CompositeResultDocument.MetaDb.cs GetElementTokenType, lines
/// 903-920, which resolves the Reference row and recomputes the chunk lookup for a row it
/// already holds a ref to), then TryUpgradeOpaqueTarget reads target.SelectionSet
/// (ValueCompletion.cs line 252 into CompositeResultDocument.cs lines 52-67, two more full
/// row reads of the same rows), then target.GetObjectContext()
/// (CompositeResultDocument.TryGetProperty.cs lines 370-390) resolves the same Reference a
/// third time via MetaDb.GetValue (MetaDb.cs lines 752-764) and fetches the selection set
/// again. TryCompleteObjectValue (ValueCompletion.cs lines 1116-1126) pays ValueKind plus
/// GetObjectContext per re-merged nested object.
///
/// List path: TryCompleteList (ValueCompletion.cs lines 935-957) decodes the same target
/// row five times on the merge branch: ValueKind at line 935, ValueKind again at line 939,
/// GetArrayLength at line 940, the EnumerateArray token re-check
/// (CompositeResultElement.cs lines 969-987) and the ArrayEnumerator constructor's GetValue
/// (CompositeResultElement.ArrayEnumerator.cs lines 19-27).
///
/// The fused candidate resolves the target row exactly once per node with the real
/// MetaDb.GetValue and derives everything from that row: a tri-state object context
/// (TokenType.None reports the initialize path, StartObject yields the context plus its
/// selection set, anything else throws exactly like CheckExpectedType) and an array context
/// that surfaces the resolved token type plus length and row span so the enumerator starts
/// with zero further decodes while the non-array error branch stays non-throwing. Fresh
/// (TokenType.None) slots are benchmarked as their own pair to show the false path is cost
/// neutral, which is the main regression scenario of the candidate.
///
/// The baselines call only real product internals on a real CompositeResultDocument whose
/// targets were materialized through SetObjectValue/SetArrayValue, so every measured target
/// is a Reference row exactly like a steady-state merge target.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class CompositeContextFusedDecodeBenchmark : FusionBenchmarkBase
{
    /// <summary>
    /// BenchmarkDotNet 0.15.8 has no RuntimeMoniker for the net11.0 preview host and
    /// this project pins TargetFramework to net11.0, so out-of-process toolchains can
    /// neither validate nor build a child process here. The job therefore runs in
    /// process with the intended 3 warmup and 10 measurement iterations. Benchmarks are
    /// grouped by category so every baseline/fused pair gets its own Ratio column.
    /// </summary>
    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(
                Job.Default
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithToolchain(InProcessEmitToolchain.Instance));
            AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);
            AddColumn(CategoriesColumn.Default);
        }
    }

    private const string OperationId = "123456789101112";
    private const int ObjectCount = 1000;
    private const int ListCount = 100;
    private const int ListLength = 50;
    private const int TinyListCount = 100;
    private const int TinyListLength = 1;
    private const int FreshCount = 1000;

    private MemoryArena _arena = null!;
    private CompositeResultDocument _document = null!;
    private CompositeResultElement[] _objectTargets = null!;
    private CompositeResultElement[] _listTargets = null!;
    private CompositeResultElement[] _tinyListTargets = null!;
    private CompositeResultElement[] _freshTargets = null!;

    // Stand-in for CompositeResultDocument._disposed, which is private. The fused product
    // method would keep exactly one ObjectDisposedException.ThrowIf field-load-plus-branch
    // guard (CompositeResultDocument.TryGetProperty.cs line 372); the fused accessors below
    // pay the equivalent cost through this field so the candidate is not flattered.
    private int _disposedGuard;

    [GlobalSetup]
    public void Setup()
    {
        _disposedGuard = 0;

        var schema = CreateFusionSchema();
        var documentRewriter = new DocumentRewriter(schema);
        var operationDefinition = documentRewriter
            .RewriteDocument(
                Utf8GraphQLParser.Parse(
                    """
                    {
                      productById(id: "1") {
                        id
                        name
                        description
                        price
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
            operationDefinition);

        var productSelection = operation.RootSelectionSet.Selections[0];
        var productSelectionSet = operation.GetSelectionSet(productSelection);

        if (!productSelectionSet.TryGetSelection("dimension"u8, out var dimensionSelection))
        {
            throw new InvalidOperationException("The dimension selection is missing.");
        }

        var dimensionSelectionSet = operation.GetSelectionSet(dimensionSelection);

        _arena = new MemoryArena();
        _document = new CompositeResultDocument(_arena, operation, includeFlags: 0);

        var rootContext = _document.Data.GetObjectContext();

        if (!rootContext.TryGetProperty("productById"u8, out var productSlot, out _))
        {
            throw new InvalidOperationException("The productById slot is missing.");
        }

        productSlot.SetObjectValue(productSelectionSet, out var productContext);

        // Every target below is an array element value row that SetObjectValue/SetArrayValue
        // turned into a Reference row (AssignCompositeValue, CompositeResultDocument.cs lines
        // 519-525), matching the steady-state merge targets BuildResult and TryCompleteList
        // see after the first subgraph result initialized them. The fresh targets stay
        // TokenType.None, matching first-materialization slots.
        _objectTargets = CreateArrayElementTargets(
            productContext,
            "dimension"u8,
            ObjectCount,
            element => element.SetObjectValue(dimensionSelectionSet));
        _listTargets = CreateArrayElementTargets(
            productContext,
            "name"u8,
            ListCount,
            element => element.SetArrayValue(ListLength));
        _tinyListTargets = CreateArrayElementTargets(
            productContext,
            "price"u8,
            TinyListCount,
            element => element.SetArrayValue(TinyListLength));
        _freshTargets = CreateArrayElementTargets(
            productContext,
            "description"u8,
            FreshCount,
            initialize: null);

        VerifyEquivalence();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _disposedGuard = 1;
        _document.Dispose();
        _arena.Dispose();
    }

    /// <summary>
    /// Current product sequence of BuildResult's already-initialized branch
    /// (ValueCompletion.cs lines 82-90): ValueKind, then the TryUpgradeOpaqueTarget
    /// SelectionSet read (line 252), then GetObjectContext, three resolutions of the same
    /// Reference row and two selection-set fetches per target.
    /// </summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ObjectMerge")]
    public long ObjectMerge_Baseline()
    {
        var targets = _objectTargets;
        var sum = 0L;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];

            if (target.ValueKind is JsonValueKind.Undefined)
            {
                // InitializeTargetObject (ValueCompletion.cs line 84); never taken here.
                sum--;
            }
            else
            {
                // TryUpgradeOpaqueTarget entry read (ValueCompletion.cs line 252).
                var selectionSet = target.SelectionSet;

                if (selectionSet is { Type.Kind: TypeKind.Interface })
                {
                    // The upgrade flow; never taken for these object-typed selection sets.
                    sum -= 2;
                }

                var objectContext = target.GetObjectContext();
                sum += Consume(in objectContext) + (selectionSet?.Id ?? -3);
            }
        }

        return sum;
    }

    /// <summary>
    /// Fused candidate for the same branch: one tri-state context acquisition that also
    /// hands the selection set to the upgrade check, exactly one row resolution per target.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("ObjectMerge")]
    public long ObjectMerge_Fused()
    {
        var document = _document;
        var targets = _objectTargets;
        var sum = 0L;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];

            if (!TryGetObjectContextFused(
                document,
                target.Cursor,
                out var objectContext,
                out var selectionSet))
            {
                sum--;
            }
            else
            {
                if (selectionSet is { Type.Kind: TypeKind.Interface })
                {
                    sum -= 2;
                }

                sum += Consume(in objectContext) + (selectionSet?.Id ?? -3);
            }
        }

        return sum;
    }

    /// <summary>
    /// Current product sequence of TryCompleteObjectValue's re-merge branch
    /// (ValueCompletion.cs lines 1116-1126): ValueKind, then GetObjectContext.
    /// </summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("NestedObjectMerge")]
    public long NestedObjectMerge_Baseline()
    {
        var targets = _objectTargets;
        var sum = 0L;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];

            if (target.ValueKind is JsonValueKind.Undefined)
            {
                // The SetObjectValue initialize path (lines 1117-1121); never taken here.
                sum--;
            }
            else
            {
                var objectContext = target.GetObjectContext();
                sum += Consume(in objectContext);
            }
        }

        return sum;
    }

    /// <summary>
    /// Fused candidate for the same branch: the tri-state acquisition replaces both the
    /// ValueKind probe and the GetObjectContext re-resolution.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("NestedObjectMerge")]
    public long NestedObjectMerge_Fused()
    {
        var document = _document;
        var targets = _objectTargets;
        var sum = 0L;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];

            if (!TryGetObjectContextFused(document, target.Cursor, out var objectContext, out _))
            {
                sum--;
            }
            else
            {
                sum += Consume(in objectContext);
            }
        }

        return sum;
    }

    /// <summary>
    /// Current product sequence of TryCompleteList's merge branch (ValueCompletion.cs lines
    /// 935-957): ValueKind twice, GetArrayLength, then EnumerateArray, which re-checks the
    /// token type and whose enumerator constructor resolves the row once more. The constant
    /// list length stands in for source.GetArrayLength(), whose cost is source-side and
    /// excluded from both variants; each list is consumed through one MoveNext plus Current,
    /// the per-element loop itself being identical in both variants.
    /// </summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ListMerge")]
    public long ListMerge_Baseline()
        => ListBaselineCore(_listTargets, ListLength);

    /// <summary>
    /// Fused candidate for the same branch: one row resolution yields the token type, the
    /// length and the row span, and the enumerator is built from the resolved cursor with
    /// zero further decodes.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("ListMerge")]
    public long ListMerge_Fused()
        => ListFusedCore(_listTargets, ListLength);

    /// <summary>
    /// The list merge pair on single-element lists, the tiny shape where fixed per-node
    /// costs dominate and a fused-path regression would show.
    /// </summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TinyListMerge")]
    public long TinyListMerge_Baseline()
        => ListBaselineCore(_tinyListTargets, TinyListLength);

    /// <summary>
    /// Fused candidate on single-element lists.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("TinyListMerge")]
    public long TinyListMerge_Fused()
        => ListFusedCore(_tinyListTargets, TinyListLength);

    /// <summary>
    /// Current product classification of fresh (TokenType.None) slots: the ValueKind probe
    /// is a targeted 4-byte read with no reference to resolve, after which the real code
    /// would initialize the slot.
    /// </summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FreshSlot")]
    public long FreshSlot_Baseline()
    {
        var targets = _freshTargets;
        var sum = 0L;

        for (var i = 0; i < targets.Length; i++)
        {
            if (targets[i].ValueKind is JsonValueKind.Undefined)
            {
                // The initialize path would run here; classification stops at the probe.
                sum++;
            }
            else
            {
                sum -= 5;
            }
        }

        return sum;
    }

    /// <summary>
    /// Fused candidate on fresh slots: the tri-state acquisition reads the full 20-byte row
    /// where the baseline reads 4 bytes of the same row. This pair bounds the regression
    /// risk on fresh-object-dominant payloads and must stay close to a 1.0 ratio.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("FreshSlot")]
    public long FreshSlot_Fused()
    {
        var document = _document;
        var targets = _freshTargets;
        var sum = 0L;

        for (var i = 0; i < targets.Length; i++)
        {
            if (!TryGetObjectContextFused(document, targets[i].Cursor, out _, out _))
            {
                sum++;
            }
            else
            {
                sum -= 5;
            }
        }

        return sum;
    }

    private long ListBaselineCore(CompositeResultElement[] targets, int expectedLength)
    {
        var sum = 0L;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];

            // Decode 1 (ValueCompletion.cs line 935).
            if (target.ValueKind is JsonValueKind.Undefined)
            {
                // The SetArrayValue initialize path (line 937); never taken here.
                sum--;
            }

            // Decodes 2 and 3 (lines 939-940).
            else if (target.ValueKind is not JsonValueKind.Array
                || target.GetArrayLength() != expectedLength)
            {
                // The non-throwing length-mismatch error branch (lines 941-953);
                // never taken here.
                sum -= 2;
            }
            else
            {
                // Decodes 4 and 5 (line 957): EnumerateArray re-checks the token type and
                // the ArrayEnumerator constructor resolves the row again.
                var enumerator = target.EnumerateArray().GetEnumerator();

                if (enumerator.MoveNext())
                {
                    sum += enumerator.Current.Cursor.Value;
                }
            }
        }

        return sum;
    }

    private long ListFusedCore(CompositeResultElement[] targets, int expectedLength)
    {
        var document = _document;
        var sum = 0L;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];

            var tokenType = GetArrayContextFused(
                document,
                target.Cursor,
                out var startCursor,
                out var length,
                out var numberOfRows);

            if (tokenType is ElementTokenType.None)
            {
                sum--;
            }
            else if (tokenType is not ElementTokenType.StartArray || length != expectedLength)
            {
                sum -= 2;
            }
            else
            {
                var enumerator =
                    new FusedArrayEnumerator(document, startCursor, numberOfRows).GetEnumerator();

                if (enumerator.MoveNext())
                {
                    sum += enumerator.Current.Cursor.Value;
                }
            }
        }

        return sum;
    }

    /// <summary>
    /// Candidate fused object classification. One real MetaDb.GetValue
    /// (CompositeResultDocument.MetaDb.cs lines 752-764) replaces the ValueKind probe, the
    /// SelectionSet read and the GetObjectContext re-resolution: TokenType.None reports the
    /// initialize path, StartObject yields the context built from the row already in hand
    /// (mirroring CompositeResultDocument.GetObjectContext,
    /// CompositeResultDocument.TryGetProperty.cs lines 370-390) plus its selection set for
    /// the upgrade check, and any other token throws exactly like CheckExpectedType.
    /// </summary>
    private bool TryGetObjectContextFused(
        CompositeResultDocument document,
        CompositeCursor cursor,
        out CompositeObjectContext objectContext,
        out SelectionSet? selectionSet)
    {
        // Mirrors CompositeResultElement.CheckValidInstance
        // (CompositeResultElement.cs lines 1099-1105).
        if (document is null)
        {
            throw new InvalidOperationException();
        }

        ObjectDisposedException.ThrowIf(_disposedGuard != 0, document);

        var row = document._metaDb.GetValue(ref cursor);

        if (row.TokenType is ElementTokenType.None)
        {
            objectContext = default;
            selectionSet = null;
            return false;
        }

        CheckExpectedTypeCopy(ElementTokenType.StartObject, row.TokenType);

        // Mirrors the selection-set derivation of CompositeResultDocument.GetObjectContext
        // (CompositeResultDocument.TryGetProperty.cs lines 382-387).
        selectionSet = row.OperationReferenceType is OperationReferenceType.SelectionSet
            ? document.GetOperation().GetSelectionSetById(row.OperationReferenceId)
            : null;

        objectContext = new CompositeObjectContext(
            document,
            cursor,
            selectionSet,
            row.NumberOfRows);
        return true;
    }

    /// <summary>
    /// Candidate fused list classification. One real MetaDb.GetValue resolves the target
    /// and surfaces the token type so the caller keeps TryCompleteList's non-throwing
    /// length-mismatch error branch (ValueCompletion.cs lines 939-954) exactly as today;
    /// length and row span are only derived for StartArray rows.
    /// </summary>
    private ElementTokenType GetArrayContextFused(
        CompositeResultDocument document,
        CompositeCursor cursor,
        out CompositeCursor startCursor,
        out int length,
        out int numberOfRows)
    {
        // Mirrors CompositeResultElement.CheckValidInstance
        // (CompositeResultElement.cs lines 1099-1105).
        if (document is null)
        {
            throw new InvalidOperationException();
        }

        ObjectDisposedException.ThrowIf(_disposedGuard != 0, document);

        var row = document._metaDb.GetValue(ref cursor);
        startCursor = cursor;

        if (row.TokenType is ElementTokenType.StartArray)
        {
            length = row.SizeOrLength;
            numberOfRows = row.NumberOfRows;
            return ElementTokenType.StartArray;
        }

        length = 0;
        numberOfRows = 0;
        return row.TokenType;
    }

    // Byte-faithful copy of the private CompositeResultDocument.CheckExpectedType
    // (CompositeResultDocument.cs lines 590-596), which the fused product method would call.
    private static void CheckExpectedTypeCopy(ElementTokenType expected, ElementTokenType actual)
    {
        if (expected != actual)
        {
            throw new ArgumentOutOfRangeException($"Expected {expected} but found {actual}.");
        }
    }

    /// <summary>
    /// Opaque sink that forces both variants to fully materialize the acquired context so
    /// neither side can be dead-code eliminated.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Consume(in CompositeObjectContext objectContext) => 1;

    /// <summary>
    /// Candidate enumerator shape: mirrors CompositeResultElement.ArrayEnumerator
    /// (CompositeResultElement.ArrayEnumerator.cs) except that the constructor takes the
    /// pre-resolved start cursor and row count instead of re-running MetaDb.GetValue
    /// (lines 19-27), which is exactly the internal constructor the recommended design adds.
    /// </summary>
    private struct FusedArrayEnumerator
    {
        private readonly CompositeResultDocument _document;
        private readonly CompositeCursor _start;
        private readonly CompositeCursor _end;
        private CompositeCursor _cursor;

        internal FusedArrayEnumerator(
            CompositeResultDocument document,
            CompositeCursor startCursor,
            int numberOfRows)
        {
            _document = document;
            _start = startCursor;
            _end = startCursor + numberOfRows;
            _cursor = startCursor;
        }

        // Mirrors ArrayEnumerator.Current (lines 30-42).
        public CompositeResultElement Current
        {
            get
            {
                var cursor = _cursor;
                if (cursor == _start || cursor >= _end)
                {
                    return default;
                }

                return new CompositeResultElement(_document, cursor);
            }
        }

        // Mirrors ArrayEnumerator.GetEnumerator (lines 50-55).
        public FusedArrayEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator._cursor = enumerator._start;
            return enumerator;
        }

        // Mirrors ArrayEnumerator.MoveNext (lines 66-93).
        public bool MoveNext()
        {
            var start = _start;
            var end = _end;

            if (_cursor == start)
            {
                var first = start + 1;
                if (first < end)
                {
                    _cursor = first;
                    return true;
                }

                _cursor = end;
                return false;
            }

            var next = _cursor + 1;
            if (next < end)
            {
                _cursor = next;
                return true;
            }

            _cursor = end;
            return false;
        }
    }

    private static CompositeResultElement[] CreateArrayElementTargets(
        in CompositeObjectContext parentContext,
        ReadOnlySpan<byte> slotName,
        int count,
        Action<CompositeResultElement>? initialize)
    {
        if (!parentContext.TryGetProperty(slotName, out var slot, out _))
        {
            throw new InvalidOperationException("A benchmark slot is missing.");
        }

        slot.SetArrayValue(count);

        var targets = new CompositeResultElement[count];
        var i = 0;

        foreach (var element in slot.EnumerateArray())
        {
            initialize?.Invoke(element);
            targets[i++] = element;
        }

        if (i != count)
        {
            throw new InvalidOperationException("Target materialization is incomplete.");
        }

        return targets;
    }

    private void VerifyEquivalence()
    {
        for (var i = 0; i < _objectTargets.Length; i++)
        {
            VerifyObjectTarget(_objectTargets[i], i);
        }

        for (var i = 0; i < _listTargets.Length; i++)
        {
            VerifyListTarget(_listTargets[i], ListLength, i);
        }

        for (var i = 0; i < _tinyListTargets.Length; i++)
        {
            VerifyListTarget(_tinyListTargets[i], TinyListLength, i);
        }

        for (var i = 0; i < _freshTargets.Length; i++)
        {
            VerifyFreshTarget(_freshTargets[i], i);
        }

        VerifyChecksum("ObjectMerge", ObjectMerge_Baseline(), ObjectMerge_Fused());
        VerifyChecksum("NestedObjectMerge", NestedObjectMerge_Baseline(), NestedObjectMerge_Fused());
        VerifyChecksum("ListMerge", ListMerge_Baseline(), ListMerge_Fused());
        VerifyChecksum("TinyListMerge", TinyListMerge_Baseline(), TinyListMerge_Fused());
        VerifyChecksum("FreshSlot", FreshSlot_Baseline(), FreshSlot_Fused());
    }

    private void VerifyObjectTarget(CompositeResultElement target, int index)
    {
        if (target.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Object target {index} is not an object but {target.ValueKind}.");
        }

        var baselineSelectionSet = target.SelectionSet;
        var baselineContext = target.GetObjectContext();

        if (!TryGetObjectContextFused(
            _document,
            target.Cursor,
            out var fusedContext,
            out var fusedSelectionSet))
        {
            throw new InvalidOperationException(
                $"The fused acquisition reported object target {index} as uninitialized.");
        }

        if (!ReferenceEquals(baselineSelectionSet, fusedSelectionSet))
        {
            throw new InvalidOperationException(
                $"Object target {index} resolves different selection sets between the "
                + "baseline and the fused variant.");
        }

        CompareContextProperty(in baselineContext, in fusedContext, "height"u8, index);
        CompareContextProperty(in baselineContext, in fusedContext, "width"u8, index);
        CompareContextProperty(in baselineContext, in fusedContext, "missing"u8, index);
    }

    private static void CompareContextProperty(
        in CompositeObjectContext baselineContext,
        in CompositeObjectContext fusedContext,
        ReadOnlySpan<byte> propertyName,
        int index)
    {
        var baselineFound = baselineContext.TryGetProperty(
            propertyName,
            out var baselineValue,
            out var baselineSelection);
        var fusedFound = fusedContext.TryGetProperty(
            propertyName,
            out var fusedValue,
            out var fusedSelection);

        if (baselineFound != fusedFound
            || baselineValue.Cursor != fusedValue.Cursor
            || !ReferenceEquals(baselineSelection, fusedSelection))
        {
            throw new InvalidOperationException(
                $"Object target {index} resolves a property differently between the "
                + "baseline context and the fused context.");
        }
    }

    private void VerifyListTarget(CompositeResultElement target, int expectedLength, int index)
    {
        if (target.ValueKind is not JsonValueKind.Array
            || target.GetArrayLength() != expectedLength)
        {
            throw new InvalidOperationException(
                $"List target {index} is not an array of length {expectedLength}.");
        }

        var tokenType = GetArrayContextFused(
            _document,
            target.Cursor,
            out var startCursor,
            out var length,
            out var numberOfRows);

        if (tokenType is not ElementTokenType.StartArray || length != expectedLength)
        {
            throw new InvalidOperationException(
                $"The fused acquisition misclassifies list target {index}: "
                + $"{tokenType} with length {length}.");
        }

        var baselineEnumerator = target.EnumerateArray().GetEnumerator();
        var fusedEnumerator =
            new FusedArrayEnumerator(_document, startCursor, numberOfRows).GetEnumerator();
        var elementCount = 0;

        while (true)
        {
            var baselineMoved = baselineEnumerator.MoveNext();
            var fusedMoved = fusedEnumerator.MoveNext();

            if (baselineMoved != fusedMoved)
            {
                throw new InvalidOperationException(
                    $"List target {index} enumerators disagree after {elementCount} elements.");
            }

            if (!baselineMoved)
            {
                break;
            }

            if (baselineEnumerator.Current.Cursor != fusedEnumerator.Current.Cursor)
            {
                throw new InvalidOperationException(
                    $"List target {index} yields a different element cursor at "
                    + $"position {elementCount}.");
            }

            elementCount++;
        }

        if (elementCount != expectedLength)
        {
            throw new InvalidOperationException(
                $"List target {index} enumerated {elementCount} elements "
                + $"instead of {expectedLength}.");
        }
    }

    private void VerifyFreshTarget(CompositeResultElement target, int index)
    {
        if (target.ValueKind is not JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Fresh target {index} is not undefined.");
        }

        if (TryGetObjectContextFused(_document, target.Cursor, out _, out _))
        {
            throw new InvalidOperationException(
                $"The fused acquisition reported fresh target {index} as initialized.");
        }

        var tokenType = GetArrayContextFused(_document, target.Cursor, out _, out _, out _);

        if (tokenType is not ElementTokenType.None)
        {
            throw new InvalidOperationException(
                $"The fused list acquisition misclassifies fresh target {index} as {tokenType}.");
        }
    }

    private static void VerifyChecksum(string pair, long baseline, long fused)
    {
        if (baseline != fused)
        {
            throw new InvalidOperationException(
                $"{pair} checksum mismatch: baseline {baseline} vs fused {fused}.");
        }
    }
}
