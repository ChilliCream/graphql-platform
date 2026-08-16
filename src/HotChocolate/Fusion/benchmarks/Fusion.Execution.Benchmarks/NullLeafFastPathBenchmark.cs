using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures the merge-loop cost of source properties whose JSON value is null.
///
/// The scalar fast path of the four BuildResult/TryCompleteObjectValue property loops
/// (ValueCompletion.cs lines 129, 182, 1218 and 1264) only covers String, Number, True
/// and False (ValueCompletionExtensions.IsScalarValue, lines 1477-1479), so a null-valued
/// property always takes the slow route: a <c>ResultSelectionSet.TryGetChild</c> response
/// name lookup (lines 147, 200, 1236, 1282) whose result the null path never consumes,
/// then a non-inlined <c>TryCompleteValue</c> call whose null handling (lines 808-872)
/// ends in a single <c>SetNullValue</c> or, for value-type named types, no write at all.
///
/// The candidate widens the fast-path kind test from String..False to String..Null
/// (still one range compare, the kinds are contiguous) and handles null inside the
/// fast path: nullable selections get a direct <c>SetNullValue</c> (or, for value-type
/// named types, no write), reproducing the errorTrie-null null semantics of
/// TryCompleteValue exactly, while IsNonNull nulls fall through to the
/// error-generating slow path. An earlier variant that added a separate null branch
/// after the scalar fast path regressed the all-scalar shape by 4-6%; folding the
/// test into the existing range check is the redesign. The baseline drives the real
/// <c>ResultSelectionSet.TryGetChild</c> plus a byte-faithful local copy of the
/// TryCompleteValue null branches; every other operation in both loops (property lookup,
/// snapshot, scalar completion) is the same real product internal on both sides.
///
/// Shapes: AllNull (every property null, isolates the per-null saving), Half (2 of 4
/// null on a PageInfo-like set), Mixed (1 of 4 null on the productById set), and Dense
/// (0 nulls, the regression gate for the added branch).
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class NullLeafFastPathBenchmark : FusionBenchmarkBase
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
    private const int SourceObjectCount = 1000;

    private const string ProductSourceText =
        """
        {
          id
          name
          description
          price
          dimension { height width }
        }
        """;

    private const string PageInfoSourceText =
        """
        {
          hasNextPage
          hasPreviousPage
          startCursor
          endCursor
        }
        """;

    [Params("AllNull", "Half", "Mixed", "Dense")]
    public string Shape = "AllNull";

    private MemoryArena _arena = null!;
    private CompositeResultDocument _document = null!;
    private SourceResultDocument _sourceDocument = null!;
    private SourceResultElement[] _sourceElements = null!;
    private CompositeObjectContext _targetContext;
    private ResultSelectionSet _resultSelectionSet = null!;

    // Never assigned: stands in for BuildResult's errorTrie parameter so the
    // null-conditional at ValueCompletion.cs line 198 stays a real runtime check.
#pragma warning disable CS0649
    private ErrorTrie? _errorTrie;
#pragma warning restore CS0649

    public long Consumed;

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
                        id
                        name
                        description
                        price
                        dimension { height width }
                      }
                      products(first: 10) {
                        pageInfo {
                          hasNextPage
                          hasPreviousPage
                          startCursor
                          endCursor
                        }
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

        var productSelection = operation.RootSelectionSet.Selections[0];
        var productSelectionSet = operation.GetSelectionSet(productSelection);

        if (!rootContext.TryGetProperty("productById"u8, out var productSlot, out _))
        {
            throw new InvalidOperationException("The productById slot is missing.");
        }

        productSlot.SetObjectValue(productSelectionSet, out var productContext);

        var productsSelection = operation.RootSelectionSet.Selections[1];
        var connectionSelectionSet = operation.GetSelectionSet(productsSelection);

        if (!connectionSelectionSet.TryGetSelection("pageInfo"u8, out var pageInfoSelection))
        {
            throw new InvalidOperationException("The pageInfo selection is missing.");
        }

        var pageInfoSelectionSet = operation.GetSelectionSet(pageInfoSelection);

        if (!rootContext.TryGetProperty("products"u8, out var productsSlot, out _))
        {
            throw new InvalidOperationException("The products slot is missing.");
        }

        productsSlot.SetObjectValue(connectionSelectionSet, out var connectionContext);

        if (!connectionContext.TryGetProperty("pageInfo"u8, out var pageInfoSlot, out _))
        {
            throw new InvalidOperationException("The pageInfo slot is missing.");
        }

        pageInfoSlot.SetObjectValue(pageInfoSelectionSet, out var pageInfoContext);

        var productResultSet = ResultSelectionSet.Create(
            Utf8GraphQLParser.Syntax.ParseSelectionSet(ProductSourceText),
            schema);
        var pageInfoResultSet = ResultSelectionSet.Create(
            Utf8GraphQLParser.Syntax.ParseSelectionSet(PageInfoSourceText),
            schema);

        (var sourceObject, _targetContext, _resultSelectionSet) = Shape switch
        {
            "AllNull" => (
                """{"startCursor":null,"endCursor":null}""",
                pageInfoContext,
                pageInfoResultSet),
            "Half" => (
                """{"hasNextPage":true,"hasPreviousPage":false,"startCursor":null,"endCursor":null}""",
                pageInfoContext,
                pageInfoResultSet),
            "Mixed" => (
                """{"id":"1","name":"Widget","description":null,"price":19.99}""",
                productContext,
                productResultSet),
            "Dense" => (
                """{"id":"1","name":"Widget","description":"About the widget","price":19.99}""",
                productContext,
                productResultSet),
            _ => throw new InvalidOperationException($"Unknown shape {Shape}.")
        };

        var payload = new StringBuilder("[");

        for (var i = 0; i < SourceObjectCount; i++)
        {
            if (i > 0)
            {
                payload.Append(',');
            }

            payload.Append(sourceObject);
        }

        payload.Append(']');

        var json = Encoding.UTF8.GetBytes(payload.ToString());
        _sourceDocument = SourceResultDocument.Parse(_arena, json, json.Length);

        _sourceElements = new SourceResultElement[SourceObjectCount];
        var index = 0;

        foreach (var element in _sourceDocument.Root.EnumerateArray())
        {
            _sourceElements[index++] = element;
        }

        if (index != SourceObjectCount)
        {
            throw new InvalidOperationException("Source materialization is incomplete.");
        }

        VerifyShapeAssumptions();
        VerifyEquivalence();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _document.Dispose();
        _sourceDocument.Dispose();
        _arena.Dispose();
    }

    /// <summary>
    /// The claims of the benchmark depend on which selections are nullable leaves; pin
    /// them so schema drift fails the run instead of silently changing the shape.
    /// </summary>
    private void VerifyShapeAssumptions()
    {
        foreach (var element in _sourceElements)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!_targetContext.TryGetProperty(property.NameSpan, out _, out var selection))
                {
                    throw new InvalidOperationException(
                        $"Source property {property.Name} has no target selection.");
                }

                var kind = property.Value.ValueKind;

                if (kind is JsonValueKind.Null)
                {
                    if (selection.IsNonNull)
                    {
                        throw new InvalidOperationException(
                            $"Null source property {property.Name} targets a non-null "
                            + "selection; the benchmark shapes must stay error-free.");
                    }
                }
                else if (!IsScalarValueCopy(kind))
                {
                    throw new InvalidOperationException(
                        $"Source property {property.Name} is neither scalar nor null; "
                        + "the shapes must route every non-null value through the "
                        + "scalar fast path.");
                }
                else if (selection.IsEnumValue)
                {
                    throw new InvalidOperationException(
                        $"Source property {property.Name} targets an enum selection; "
                        + "the shapes must not require enum completion.");
                }
            }
        }
    }

    /// <summary>
    /// Runs one merge round through both kernels and asserts every target slot ends in
    /// the same state: same value kind, and for scalar slots the same raw source bytes.
    /// </summary>
    private void VerifyEquivalence()
    {
        var element = _sourceElements[0];

        Consumed += MergeBaseline(element);
        var baselineStates = CaptureSlotStates(element);

        Consumed += MergeWithNullFastPath(element);
        var fastPathStates = CaptureSlotStates(element);

        if (baselineStates.Count != fastPathStates.Count)
        {
            throw new InvalidOperationException("The kernels observed different slots.");
        }

        for (var i = 0; i < baselineStates.Count; i++)
        {
            if (baselineStates[i] != fastPathStates[i])
            {
                throw new InvalidOperationException(
                    $"Slot state mismatch after the merge round: baseline "
                    + $"{baselineStates[i]} vs fast path {fastPathStates[i]}.");
            }
        }
    }

    private List<string> CaptureSlotStates(SourceResultElement element)
    {
        var states = new List<string>();

        foreach (var property in element.EnumerateObject())
        {
            if (!_targetContext.TryGetProperty(property.NameSpan, out var resultField, out _))
            {
                continue;
            }

            states.Add($"{property.Name}:{resultField.ValueKind}");
        }

        return states;
    }

    [Benchmark(Baseline = true)]
    public long Merge_Baseline()
    {
        var sum = 0L;

        for (var i = 0; i < _sourceElements.Length; i++)
        {
            sum += MergeBaseline(_sourceElements[i]);
        }

        return sum;
    }

    [Benchmark]
    public long Merge_NullFastPath()
    {
        var sum = 0L;

        for (var i = 0; i < _sourceElements.Length; i++)
        {
            sum += MergeWithNullFastPath(_sourceElements[i]);
        }

        return sum;
    }

    /// <summary>
    /// Layout-noise control: an exact textual copy of the baseline kernel in its own
    /// method body. Any Ratio deviation of this row from 1.00 bounds the code-layout
    /// noise floor of the harness, which the candidate's Dense delta must be judged
    /// against.
    /// </summary>
    [Benchmark]
    public long Merge_BaselineCopy()
    {
        var sum = 0L;

        for (var i = 0; i < _sourceElements.Length; i++)
        {
            sum += MergeBaselineCopy(_sourceElements[i]);
        }

        return sum;
    }

    /// <summary>
    /// The current per-property loop of BuildResult's non-mapping branch
    /// (ValueCompletion.cs lines 169-222) restricted to errorTrie == null: real
    /// property lookup, real snapshot, real scalar fast path, then the real
    /// TryGetChild plus the TryCompleteValue null handling for null values.
    /// </summary>
    private long MergeBaseline(SourceResultElement source)
    {
        var objectContext = _targetContext;
        var resultSelectionSet = _resultSelectionSet;
        var errorTrie = _errorTrie;
        var sum = 0L;

        foreach (var property in source.EnumerateObject())
        {
            if (!objectContext.TryGetProperty(property.NameSpan, out var resultField, out var selection))
            {
                continue;
            }

            var propertyValueSnapshot = property.Value.CreateSnapshot();
            var propertyValueKind = propertyValueSnapshot.ValueKind;

            if (errorTrie is null && IsScalarValueCopy(propertyValueKind))
            {
                if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                {
                    // CompleteEnumValue (line 186); excluded by VerifyShapeAssumptions.
                    throw new InvalidOperationException("Unreachable enum completion.");
                }

                resultField.SetLeafValue(propertyValueSnapshot);
                continue;
            }

            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            var childSet = resultSelectionSet.TryGetChild(selection.ResponseName);
            if (!CompleteValueNullPathCopy(
                    propertyValueSnapshot,
                    resultField,
                    errorTrieForResponseName,
                    selection,
                    0,
                    childSet))
            {
                sum--;
            }
        }

        return sum;
    }

    /// <summary>
    /// Exact textual copy of <see cref="MergeBaseline"/> for the layout-noise control.
    /// </summary>
    private long MergeBaselineCopy(SourceResultElement source)
    {
        var objectContext = _targetContext;
        var resultSelectionSet = _resultSelectionSet;
        var errorTrie = _errorTrie;
        var sum = 0L;

        foreach (var property in source.EnumerateObject())
        {
            if (!objectContext.TryGetProperty(property.NameSpan, out var resultField, out var selection))
            {
                continue;
            }

            var propertyValueSnapshot = property.Value.CreateSnapshot();
            var propertyValueKind = propertyValueSnapshot.ValueKind;

            if (errorTrie is null && IsScalarValueCopy(propertyValueKind))
            {
                if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                {
                    // CompleteEnumValue (line 186); excluded by VerifyShapeAssumptions.
                    throw new InvalidOperationException("Unreachable enum completion.");
                }

                resultField.SetLeafValue(propertyValueSnapshot);
                continue;
            }

            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            var childSet = resultSelectionSet.TryGetChild(selection.ResponseName);
            if (!CompleteValueNullPathCopy(
                    propertyValueSnapshot,
                    resultField,
                    errorTrieForResponseName,
                    selection,
                    0,
                    childSet))
            {
                sum--;
            }
        }

        return sum;
    }

    /// <summary>
    /// The candidate loop: identical to <see cref="MergeBaseline"/> except the fast
    /// path also covers null, completing nullable-selection nulls in place.
    /// </summary>
    private long MergeWithNullFastPath(SourceResultElement source)
    {
        var objectContext = _targetContext;
        var resultSelectionSet = _resultSelectionSet;
        var errorTrie = _errorTrie;
        var sum = 0L;

        foreach (var property in source.EnumerateObject())
        {
            if (!objectContext.TryGetProperty(property.NameSpan, out var resultField, out var selection))
            {
                continue;
            }

            var propertyValueSnapshot = property.Value.CreateSnapshot();
            var propertyValueKind = propertyValueSnapshot.ValueKind;

            // The candidate fast path: one contiguous range test covers scalars and
            // null. Scalars complete exactly as today; nullable-selection nulls
            // reproduce TryCompleteValue's errorTrie-null null semantics
            // (ValueCompletion.cs lines 838-871) without the child lookup or the
            // call, while IsNonNull nulls fall through to the slow path so error
            // creation and null propagation stay unchanged.
            if (errorTrie is null && IsScalarOrNullValueCopy(propertyValueKind))
            {
                if (propertyValueKind is not JsonValueKind.Null)
                {
                    if (propertyValueKind is JsonValueKind.String && selection.IsEnumValue)
                    {
                        throw new InvalidOperationException("Unreachable enum completion.");
                    }

                    resultField.SetLeafValue(propertyValueSnapshot);
                    continue;
                }

                if (!selection.IsNonNull)
                {
                    if (!selection.IsValueTypeNamedType)
                    {
                        resultField.SetNullValue();
                    }

                    continue;
                }
            }

            ErrorTrie? errorTrieForResponseName = null;
            errorTrie?.TryGetValue(selection.ResponseName, out errorTrieForResponseName);

            var childSet = resultSelectionSet.TryGetChild(selection.ResponseName);
            if (!CompleteValueNullPathCopy(
                    propertyValueSnapshot,
                    resultField,
                    errorTrieForResponseName,
                    selection,
                    0,
                    childSet))
            {
                sum--;
            }
        }

        return sum;
    }

    /// <summary>
    /// Byte-faithful copy of the branches of the private
    /// <c>ValueCompletion.TryCompleteValue</c> (ValueCompletion.cs lines 800-872) that a
    /// null source value can reach with errorTrie == null; the branches the benchmark
    /// shapes exclude throw instead of silently diverging. NoInlining mirrors the real
    /// call boundary: the product method is far above the inlining budget, so every
    /// null-valued property pays the call with its six arguments including the
    /// by-value snapshot struct.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CompleteValueNullPathCopy(
        SourceResultElementSnapshot source,
        CompositeResultElement target,
        ErrorTrie? errorTrie,
        Selection selection,
        int depth,
        ResultSelectionSet? resultSelectionSet)
    {
        _ = depth;

        // lines 808-809
        var sourceValueKind = source.ValueKind;
        var isNullOrUndefined = sourceValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

        // lines 811-836: the non-null violation error path
        if (selection.IsNonNull && isNullOrUndefined)
        {
            throw new InvalidOperationException(
                "Unreachable: the shapes never feed nulls into non-null selections.");
        }

        // lines 838-872
        if (isNullOrUndefined)
        {
            if (sourceValueKind is JsonValueKind.Null && selection.IsValueTypeNamedType)
            {
                if (errorTrie?.FindFirstError() is { } && resultSelectionSet is not null)
                {
                    // lines 842-846: PocketErrors, unreachable with errorTrie == null.
                    throw new InvalidOperationException("Unreachable pocket-error path.");
                }

                // For shared parent types we keep the target untouched so that
                // sibling subgraph results can still initialize and populate it.
                return true;
            }

            if (errorTrie?.FindFirstError() is { })
            {
                // lines 857-865, unreachable with errorTrie == null.
                throw new InvalidOperationException("Unreachable error path.");
            }

            target.SetNullValue();
            return true;
        }

        // lines 874 onward: list/object/leaf completion, unreachable because the shapes
        // route every non-null value through the scalar fast path before this call.
        throw new InvalidOperationException(
            "Unreachable: only null values reach the replica in this benchmark.");
    }

    /// <summary>
    /// Copy of the file-local <c>ValueCompletionExtensions.IsScalarValue</c>
    /// (ValueCompletion.cs lines 1477-1479).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsScalarValueCopy(JsonValueKind valueKind)
        => valueKind is JsonValueKind.String or JsonValueKind.Number
            or JsonValueKind.True or JsonValueKind.False;

    /// <summary>
    /// The candidate's widened fast-path test: String, Number, True, False and Null
    /// are contiguous enum members, so this stays a single range compare.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsScalarOrNullValueCopy(JsonValueKind valueKind)
        => valueKind is JsonValueKind.String or JsonValueKind.Number
            or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null;
}
