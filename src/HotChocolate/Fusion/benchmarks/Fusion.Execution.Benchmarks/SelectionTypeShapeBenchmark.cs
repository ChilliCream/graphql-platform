using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures the per-field type-shape derivation that <c>ValueCompletion</c> repeats for
/// every completion that misses the scalar fast path. Every call site passes
/// <c>selection.Type</c>, which re-dispatches through <c>IOutputFieldDefinition.Type</c>
/// on each read (Selection.cs line 100; call sites ValueCompletion.cs lines 153, 204,
/// 1189, 1233). Inside <c>TryCompleteValue</c> the code then pays <c>type.Kind</c>
/// (line 799), <c>type.InnerType()</c> (line 828), <c>IsValueType(type)</c> with a full
/// <c>type.NamedType()</c> walk (line 833, body at lines 766-782), and a second
/// <c>switch (type.Kind)</c> (line 867). The list arm re-derives the element shape per
/// invocation (TryCompleteList lines 923-930 via the file-local <c>ElementType</c>
/// extension at lines 1422-1429), the object arm walks <c>type.NamedType()</c> again
/// (lines 1088-1089), and the abstract arm walks it once more inside <c>GetType</c>
/// (line 1276).
///
/// The candidate precomputes the shape once per <see cref="Selection"/> at operation
/// compile time: the field type, a non-null flag, the type with one NonNull wrapper
/// removed, the unwrapped type kind, a value-type bit for the named type, and the
/// level-0 list element shape. The completion path then branches on plain field reads.
/// The enum arm keeps its defensive pattern match, matching the reviewed design.
///
/// The baseline replays the exact product derivation sequence over real compiled
/// <see cref="Selection"/> instances from a composed fusion schema; the optimized
/// variant reads a benchmark-local shape holder standing in for the new Selection
/// fields (same load pattern: one object dereference plus field reads). Work that is
/// identical in both variants (error building, target writes, source row reads and
/// abstract __typename resolution) is factored out of both loops. Scalar-typed events
/// with non-null sources model error-carrying merges, because without an error trie the
/// fast path at ValueCompletion.cs lines 132/183/1168/1212 bypasses
/// <c>TryCompleteValue</c> entirely; that fast path is untouched by the candidate.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class SelectionTypeShapeBenchmark
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

    private const int EventCount = 1000;

    // Distinct constants per derivation exit so different control-flow paths
    // contribute distinguishable checksum amounts in both variants.
    private const int NonNullViolationMarker = 101;
    private const int ValueTypeNullMarker = 59;
    private const int NullValueMarker = 23;
    private const int ScalarMarker = 7;
    private const int EnumMaskMarker = 11;
    private const int ElementNonNullMarker = 17;
    private const int ElementNullableMarker = 3;

    private const string MixedWorkload = "Mixed";
    private const string NullHeavyWorkload = "NullHeavy";
    private const string MonomorphicScalarWorkload = "MonomorphicScalar";
    private const string SingleEventWorkload = "SingleEvent";

    private Selection[] _selections = null!;
    private SelectionTypeShape[] _shapes = null!;
    private Dictionary<string, CompletionEvent[]> _workloads = null!;
    private CompletionEvent[] _events = null!;

    public long Consumed;

    /// <summary>
    /// Mixed cycles all thirteen selection shapes (non-null and nullable scalars,
    /// enums, objects, lists, an interface) with an occasional null source, keeping
    /// the dispatch sites polymorphic like a real payload. NullHeavy alternates null
    /// and non-null sources over the nullable selections, stressing the
    /// IsValueType walk. MonomorphicScalar replays one non-null scalar selection so
    /// guarded devirtualization can do its best for the baseline, which bounds the
    /// win honestly. SingleEvent is the N=1 regression shape.
    /// </summary>
    [Params(MixedWorkload, NullHeavyWorkload, MonomorphicScalarWorkload, SingleEventWorkload)]
    public string Workload { get; set; } = MixedWorkload;

    [GlobalSetup]
    public void Setup()
    {
        var schema = ComposeSchema();
        var operation = CompileOperation(schema);

        CollectSelections(schema, operation);

        _shapes = new SelectionTypeShape[_selections.Length];

        for (var i = 0; i < _selections.Length; i++)
        {
            _shapes[i] = CreateShape(_selections[i]);
        }

        VerifyKindCoverage();
        BuildWorkloads();
        VerifyShapes();
        VerifyChecksums();

        _events = _workloads[Workload];
    }

    /// <summary>
    /// Current product behavior: per completion event the type shape is derived from
    /// <c>selection.Type</c> through the full interface-dispatch and type-test chain
    /// of TryCompleteValue, TryCompleteList, TryCompleteObjectValue and GetType.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long DerivePerCompletion()
    {
        var sum = RunBaseline(_events);
        Consumed = sum;
        return sum;
    }

    /// <summary>
    /// Candidate optimization: the same decisions read a per-selection shape that was
    /// computed once at operation compile time, replacing the dispatch chain with
    /// field reads and a switch over the precomputed unwrapped kind.
    /// </summary>
    [Benchmark]
    public long PrecomputedSelectionShape()
    {
        var sum = RunOptimized(_events);
        Consumed = sum;
        return sum;
    }

    private long RunBaseline(CompletionEvent[] events)
    {
        var checksum = 0L;
        var selections = _selections;

        for (var i = 0; i < events.Length; i++)
        {
            var completionEvent = events[i];
            var selection = selections[completionEvent.SelectionIndex];
            var sourceValueKind = completionEvent.SourceValueKind;

            // Call sites pass selection.Type (ValueCompletion.cs lines 153, 204, 1189,
            // 1233), an interface re-dispatch through Field.Type per read
            // (Selection.cs line 100).
            var type = selection.Type;

            // TryCompleteValue lines 796-799.
            var isNullOrUndefined =
                sourceValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

            if (type.Kind is TypeKind.NonNull)
            {
                if (isNullOrUndefined)
                {
                    // Non-null violation. The error building at lines 803-825 is
                    // identical in both variants and factored out.
                    checksum += NonNullViolationMarker;
                    continue;
                }

                // Line 828.
                type = type.InnerType();
            }

            if (isNullOrUndefined)
            {
                // Line 833: IsValueType(type) walks type.NamedType() per call.
                if (sourceValueKind is JsonValueKind.Null && IsValueTypeProduct(type))
                {
                    checksum += ValueTypeNullMarker;
                    continue;
                }

                // Error lookup and SetNullValue (lines 850-862) are identical in both
                // variants and factored out.
                checksum += NullValueMarker;
                continue;
            }

            // Line 867: second interface dispatch on type.Kind.
            switch (type.Kind)
            {
                case TypeKind.List:
                {
                    // TryCompleteList lines 923-930: per-invocation element shape
                    // derivation through the file-local ElementType extension.
                    var elementType = ElementTypeProduct(type);
                    var elementTypeKind = elementType.Kind;
                    var isNonNull = elementTypeKind is TypeKind.NonNull;

                    if (isNonNull)
                    {
                        elementTypeKind =
                            Unsafe.As<IType, NonNullType>(ref elementType).NullableType.Kind;
                    }

                    checksum += (int)elementTypeKind
                        + (isNonNull ? ElementNonNullMarker : ElementNullableMarker)
                        + RuntimeHelpers.GetHashCode(elementType);
                    break;
                }

                case TypeKind.Object:
                {
                    // TryCompleteObjectValue(Selection, IType, ...) lines 1088-1089.
                    var namedType = type.NamedType();
                    var objectType =
                        Unsafe.As<ITypeDefinition, IObjectTypeDefinition>(ref namedType);
                    checksum += RuntimeHelpers.GetHashCode(objectType);
                    break;
                }

                case TypeKind.Interface or TypeKind.Union:
                {
                    // TryCompleteAbstractValue enters GetType, whose line 1276 walks
                    // type.NamedType(). The __typename resolution that follows is
                    // identical in both variants and factored out.
                    var namedType = type.NamedType();
                    checksum += RuntimeHelpers.GetHashCode(namedType);
                    break;
                }

                case TypeKind.Scalar:
                    // Line 900: target.SetLeafValue(source) is identical in both
                    // variants and factored out.
                    checksum += ScalarMarker;
                    break;

                case TypeKind.Enum:
                    // CompleteEnumValue line 1067 re-tests the named type although the
                    // reviewed design keeps this pattern match on the slow path, so it
                    // appears identically in both variants.
                    if (selection.NamedType is FusionEnumTypeDefinition enumType)
                    {
                        checksum += RuntimeHelpers.GetHashCode(enumType);
                    }
                    else
                    {
                        checksum += EnumMaskMarker;
                    }

                    break;

                default:
                    // Line 908.
                    throw new NotSupportedException($"The type {type} is not supported.");
            }
        }

        return checksum;
    }

    private long RunOptimized(CompletionEvent[] events)
    {
        var checksum = 0L;
        var shapes = _shapes;

        for (var i = 0; i < events.Length; i++)
        {
            var completionEvent = events[i];
            var shape = shapes[completionEvent.SelectionIndex];
            var sourceValueKind = completionEvent.SourceValueKind;

            var isNullOrUndefined =
                sourceValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

            // The precomputed flag replaces the type.Kind dispatch at line 799 and
            // the InnerType() unwrap at line 828.
            if (shape.IsNonNull && isNullOrUndefined)
            {
                checksum += NonNullViolationMarker;
                continue;
            }

            if (isNullOrUndefined)
            {
                // The precomputed bit replaces the IsValueType walk at line 833.
                if (sourceValueKind is JsonValueKind.Null && shape.IsValueTypeNamedType)
                {
                    checksum += ValueTypeNullMarker;
                    continue;
                }

                checksum += NullValueMarker;
                continue;
            }

            // The precomputed kind replaces the type.Kind dispatch at line 867.
            switch (shape.UnwrappedKind)
            {
                case TypeKind.List:
                    // The level-0 element shape replaces lines 923-930.
                    checksum += (int)shape.ElementKind
                        + (shape.ElementIsNonNull ? ElementNonNullMarker : ElementNullableMarker)
                        + RuntimeHelpers.GetHashCode(shape.ElementType!);
                    break;

                case TypeKind.Object:
                    // The cached named type replaces the walk at lines 1088-1089.
                    checksum += RuntimeHelpers.GetHashCode(shape.NamedType);
                    break;

                case TypeKind.Interface or TypeKind.Union:
                    // The cached named type replaces the walk at line 1276.
                    checksum += RuntimeHelpers.GetHashCode(shape.NamedType);
                    break;

                case TypeKind.Scalar:
                    checksum += ScalarMarker;
                    break;

                case TypeKind.Enum:
                    // The reviewed design keeps the defensive pattern match here.
                    if (shape.NamedType is FusionEnumTypeDefinition enumType)
                    {
                        checksum += RuntimeHelpers.GetHashCode(enumType);
                    }
                    else
                    {
                        checksum += EnumMaskMarker;
                    }

                    break;

                default:
                    throw new NotSupportedException(
                        $"The type {shape.UnwrappedType} is not supported.");
            }
        }

        return checksum;
    }

    // Byte-faithful copy of the private static ValueCompletion.IsValueType,
    // src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Results/ValueCompletion.cs
    // lines 766-782.
    private static bool IsValueTypeProduct(IType? type)
    {
        if (type is null)
        {
            return false;
        }

        var namedType = type.NamedType();

        return namedType switch
        {
            FusionObjectTypeDefinition { IsValueType: true } => true,
            FusionInterfaceTypeDefinition { IsValueType: true } => true,
            FusionUnionTypeDefinition { IsValueType: true } => true,
            _ => false
        };
    }

    // Byte-faithful copy of the file-local ElementType extension that line 923
    // resolves to, src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Results/
    // ValueCompletion.cs lines 1422-1429. The file class is not reachable from
    // outside its file, so the copy is required.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IType ElementTypeProduct(IType type)
        => type switch
        {
            ListType listType => listType.ElementType,
            NonNullType { NullableType: ListType listType } => listType.ElementType,
            _ => throw new ArgumentException($"The type '{type}' is not a list type.", nameof(type))
        };

    // Mirrors the value-type bit the recommended design computes from the already
    // cached Selection._namedType at construction time, using the same three-arm
    // test as ValueCompletion.IsValueType (lines 775-781).
    private static bool ComputeIsValueTypeBit(ITypeDefinition namedType)
        => namedType switch
        {
            FusionObjectTypeDefinition { IsValueType: true } => true,
            FusionInterfaceTypeDefinition { IsValueType: true } => true,
            FusionUnionTypeDefinition { IsValueType: true } => true,
            _ => false
        };

    // Computes the per-selection shape exactly once, standing in for the extra
    // readonly fields the recommended design adds to the Selection constructor.
    private static SelectionTypeShape CreateShape(Selection selection)
    {
        var type = selection.Type;
        var isNonNull = type.Kind is TypeKind.NonNull;
        var unwrappedType = isNonNull ? ((NonNullType)type).NullableType : type;
        var unwrappedKind = unwrappedType.Kind;
        var namedType = selection.NamedType;
        var isValueTypeNamedType = ComputeIsValueTypeBit(namedType);

        IType? elementType = null;
        var elementKind = default(TypeKind);
        var elementIsNonNull = false;

        if (unwrappedKind is TypeKind.List)
        {
            elementType = ElementTypeProduct(unwrappedType);
            elementKind = elementType.Kind;
            elementIsNonNull = elementKind is TypeKind.NonNull;

            if (elementIsNonNull)
            {
                elementKind = ((NonNullType)elementType).NullableType.Kind;
            }
        }

        return new SelectionTypeShape(
            type,
            isNonNull,
            unwrappedType,
            unwrappedKind,
            isValueTypeNamedType,
            namedType,
            elementType,
            elementKind,
            elementIsNonNull);
    }

    private void CollectSelections(FusionSchemaDefinition schema, Operation operation)
    {
        var rootSet = operation.RootSelectionSet;

        if (!rootSet.TryGetSelection("product", out var productSelection))
        {
            throw new InvalidOperationException(
                "The compiled operation has no 'product' root selection.");
        }

        var productType = schema.Types.GetType<FusionObjectTypeDefinition>("Product");
        var productSet = operation.GetSelectionSet(productSelection, productType);

        string[] fieldNames =
        [
            "id", "name", "description", "price", "status", "statusRequired",
            "dimension", "packaging", "tags", "variants", "related", "media"
        ];

        var selections = new Selection[fieldNames.Length + 1];
        selections[0] = productSelection;

        for (var i = 0; i < fieldNames.Length; i++)
        {
            if (!productSet.TryGetSelection(fieldNames[i], out var selection))
            {
                throw new InvalidOperationException(
                    $"The Product selection set has no '{fieldNames[i]}' selection.");
            }

            selections[i + 1] = selection;
        }

        _selections = selections;
    }

    /// <summary>
    /// Guards workload strength: the selection list must cover every switch arm of
    /// TryCompleteValue plus non-null and nullable wrappers and both list element
    /// nullabilities, otherwise the benchmark silently measures a weaker mix.
    /// </summary>
    private void VerifyKindCoverage()
    {
        var hasList = false;
        var hasObject = false;
        var hasAbstract = false;
        var hasScalar = false;
        var hasEnum = false;
        var hasNonNull = false;
        var hasNullable = false;
        var hasNonNullElement = false;
        var hasNullableElement = false;

        foreach (var shape in _shapes)
        {
            switch (shape.UnwrappedKind)
            {
                case TypeKind.List:
                    hasList = true;
                    hasNonNullElement |= shape.ElementIsNonNull;
                    hasNullableElement |= !shape.ElementIsNonNull;
                    break;
                case TypeKind.Object:
                    hasObject = true;
                    break;
                case TypeKind.Interface or TypeKind.Union:
                    hasAbstract = true;
                    break;
                case TypeKind.Scalar:
                    hasScalar = true;
                    break;
                case TypeKind.Enum:
                    hasEnum = true;
                    break;
            }

            hasNonNull |= shape.IsNonNull;
            hasNullable |= !shape.IsNonNull;
        }

        if (!hasList || !hasObject || !hasAbstract || !hasScalar || !hasEnum
            || !hasNonNull || !hasNullable || !hasNonNullElement || !hasNullableElement)
        {
            throw new InvalidOperationException(
                "The compiled selections do not cover all TryCompleteValue shapes; "
                + "the schema or the operation drifted.");
        }
    }

    private void BuildWorkloads()
    {
        var mixed = new CompletionEvent[EventCount];

        for (var i = 0; i < EventCount; i++)
        {
            var selectionIndex = i % _selections.Length;
            var kind = i % 9 == 8
                ? JsonValueKind.Null
                : NonNullSourceKind(_shapes[selectionIndex]);
            mixed[i] = new CompletionEvent(selectionIndex, kind);
        }

        var nullableIndices = new List<int>();

        for (var i = 0; i < _shapes.Length; i++)
        {
            if (!_shapes[i].IsNonNull)
            {
                nullableIndices.Add(i);
            }
        }

        var nullHeavy = new CompletionEvent[EventCount];

        for (var i = 0; i < EventCount; i++)
        {
            var selectionIndex = nullableIndices[i % nullableIndices.Count];
            var kind = i % 2 == 0
                ? JsonValueKind.Null
                : NonNullSourceKind(_shapes[selectionIndex]);
            nullHeavy[i] = new CompletionEvent(selectionIndex, kind);
        }

        var nameIndex = IndexOfResponseName("name");
        var monomorphic = new CompletionEvent[EventCount];

        for (var i = 0; i < EventCount; i++)
        {
            monomorphic[i] = new CompletionEvent(nameIndex, JsonValueKind.String);
        }

        var dimensionIndex = IndexOfResponseName("dimension");
        CompletionEvent[] single = [new CompletionEvent(dimensionIndex, JsonValueKind.Object)];

        _workloads = new Dictionary<string, CompletionEvent[]>
        {
            [MixedWorkload] = mixed,
            [NullHeavyWorkload] = nullHeavy,
            [MonomorphicScalarWorkload] = monomorphic,
            [SingleEventWorkload] = single
        };
    }

    private int IndexOfResponseName(string responseName)
    {
        for (var i = 0; i < _selections.Length; i++)
        {
            if (_selections[i].ResponseName == responseName)
            {
                return i;
            }
        }

        throw new InvalidOperationException(
            $"No selection with response name '{responseName}' was collected.");
    }

    private static JsonValueKind NonNullSourceKind(SelectionTypeShape shape)
        => shape.UnwrappedKind switch
        {
            TypeKind.List => JsonValueKind.Array,
            TypeKind.Object or TypeKind.Interface or TypeKind.Union => JsonValueKind.Object,
            _ => JsonValueKind.String
        };

    /// <summary>
    /// Structural equivalence: every precomputed shape value must equal what the
    /// product derivation computes from the same Selection, by reference for types
    /// and by value for kinds and flags.
    /// </summary>
    private void VerifyShapes()
    {
        for (var i = 0; i < _selections.Length; i++)
        {
            var selection = _selections[i];
            var shape = _shapes[i];

            var typeFirstRead = selection.Type;
            var typeSecondRead = selection.Type;

            if (!ReferenceEquals(typeFirstRead, typeSecondRead))
            {
                throw new InvalidOperationException(
                    $"Selection '{selection.ResponseName}': Field.Type is not identity "
                    + "stable across reads, so caching it would change behavior.");
            }

            if (!ReferenceEquals(shape.Type, typeFirstRead))
            {
                throw new InvalidOperationException(
                    $"Selection '{selection.ResponseName}': cached Type differs.");
            }

            var isNonNull = typeFirstRead.Kind is TypeKind.NonNull;

            if (shape.IsNonNull != isNonNull)
            {
                throw new InvalidOperationException(
                    $"Selection '{selection.ResponseName}': IsNonNull flag differs.");
            }

            var unwrapped = isNonNull ? typeFirstRead.InnerType() : typeFirstRead;

            if (!ReferenceEquals(shape.UnwrappedType, unwrapped)
                || shape.UnwrappedKind != unwrapped.Kind)
            {
                throw new InvalidOperationException(
                    $"Selection '{selection.ResponseName}': unwrapped type or kind differs.");
            }

            if (shape.IsValueTypeNamedType != IsValueTypeProduct(unwrapped))
            {
                throw new InvalidOperationException(
                    $"Selection '{selection.ResponseName}': value-type bit differs.");
            }

            if (!ReferenceEquals(shape.NamedType, selection.NamedType)
                || !ReferenceEquals(selection.NamedType, unwrapped.NamedType()))
            {
                throw new InvalidOperationException(
                    $"Selection '{selection.ResponseName}': named type differs.");
            }

            if (unwrapped.Kind is TypeKind.List)
            {
                var elementType = ElementTypeProduct(unwrapped);
                var elementTypeKind = elementType.Kind;
                var elementIsNonNull = elementTypeKind is TypeKind.NonNull;

                if (elementIsNonNull)
                {
                    elementTypeKind =
                        Unsafe.As<IType, NonNullType>(ref elementType).NullableType.Kind;
                }

                if (!ReferenceEquals(shape.ElementType, elementType)
                    || shape.ElementKind != elementTypeKind
                    || shape.ElementIsNonNull != elementIsNonNull)
                {
                    throw new InvalidOperationException(
                        $"Selection '{selection.ResponseName}': element shape differs.");
                }
            }
            else if (shape.ElementType is not null)
            {
                throw new InvalidOperationException(
                    $"Selection '{selection.ResponseName}': non-list selection carries "
                    + "an element shape.");
            }
        }
    }

    /// <summary>
    /// Checksum equivalence over every workload, not only the active parameter:
    /// both variants must take identical control-flow paths and consume identical
    /// type instances for every event.
    /// </summary>
    private void VerifyChecksums()
    {
        foreach (var (name, events) in _workloads)
        {
            var baseline = RunBaseline(events);
            var optimized = RunOptimized(events);

            if (baseline != optimized)
            {
                throw new InvalidOperationException(
                    $"Checksum mismatch for workload '{name}': baseline {baseline} vs "
                    + $"optimized {optimized}.");
            }
        }
    }

    private static Operation CompileOperation(FusionSchemaDefinition schema)
    {
        var document = Utf8GraphQLParser.Parse(
            """
            {
              product(id: "1") {
                id
                name
                description
                price
                status
                statusRequired
                dimension { height width }
                packaging { height width }
                tags
                variants { id name }
                related { id title }
                media { id title }
              }
            }
            """);

        var documentRewriter = new DocumentRewriter(schema);
        var operationDefinition =
            documentRewriter.RewriteDocument(document).GetOperation(operationName: null);

        var pool = new NoOpObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>();
        var compiler = new OperationCompiler(schema, pool);

        return compiler.Compile("bench", "bench", "bench", operationDefinition);
    }

    /// <summary>
    /// Composes a single-source fusion schema whose Product type covers every shape
    /// TryCompleteValue dispatches over: non-null and nullable scalars, enums,
    /// objects, lists with non-null and nullable elements, and an interface.
    /// Composition recipe follows FusionBenchmarkBase.CreateFusionSchema.
    /// </summary>
    private static FusionSchemaDefinition ComposeSchema()
    {
        List<SourceSchemaText> sourceSchemas =
        [
            new SourceSchemaText(
                "catalog",
                """
                type Query {
                  product(id: ID!): Product
                  search(query: String!): [SearchResult]!
                }

                interface SearchResult {
                  id: ID!
                  title: String!
                }

                type Product implements SearchResult {
                  id: ID!
                  title: String!
                  name: String!
                  description: String
                  price: Float
                  status: ProductStatus
                  statusRequired: ProductStatus!
                  dimension: ProductDimension
                  packaging: ProductDimension!
                  tags: [String!]
                  variants: [Product]
                  related: SearchResult
                  media: [SearchResult!]!
                }

                type Article implements SearchResult {
                  id: ID!
                  title: String!
                }

                type ProductDimension {
                  height: Int!
                  width: Int!
                }

                enum ProductStatus {
                  ACTIVE
                  DISCONTINUED
                  ARCHIVED
                }
                """)
        ];

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();
        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private readonly struct CompletionEvent
    {
        public CompletionEvent(int selectionIndex, JsonValueKind sourceValueKind)
        {
            SelectionIndex = selectionIndex;
            SourceValueKind = sourceValueKind;
        }

        public readonly int SelectionIndex;

        public readonly JsonValueKind SourceValueKind;
    }

    /// <summary>
    /// Benchmark-local stand-in for the readonly fields the recommended design adds
    /// to <see cref="Selection"/>: the cached field type, the non-null flag, the type
    /// with one NonNull wrapper removed, its kind, the value-type bit, the cached
    /// named type, and the level-0 list element shape. A class keeps the optimized
    /// read pattern identical to production: one object dereference plus field reads.
    /// In the product the kind fields would be packed as bytes; field width does not
    /// change the load cost measured here.
    /// </summary>
    private sealed class SelectionTypeShape
    {
        public SelectionTypeShape(
            IType type,
            bool isNonNull,
            IType unwrappedType,
            TypeKind unwrappedKind,
            bool isValueTypeNamedType,
            ITypeDefinition namedType,
            IType? elementType,
            TypeKind elementKind,
            bool elementIsNonNull)
        {
            Type = type;
            IsNonNull = isNonNull;
            UnwrappedType = unwrappedType;
            UnwrappedKind = unwrappedKind;
            IsValueTypeNamedType = isValueTypeNamedType;
            NamedType = namedType;
            ElementType = elementType;
            ElementKind = elementKind;
            ElementIsNonNull = elementIsNonNull;
        }

        public readonly IType Type;

        public readonly bool IsNonNull;

        public readonly IType UnwrappedType;

        public readonly TypeKind UnwrappedKind;

        public readonly bool IsValueTypeNamedType;

        public readonly ITypeDefinition NamedType;

        public readonly IType? ElementType;

        public readonly TypeKind ElementKind;

        public readonly bool ElementIsNonNull;
    }

    private sealed class NoOpObjectPool<T> : ObjectPool<T> where T : class, new()
    {
        public override T Get() => new();

        public override void Return(T obj)
        {
        }
    }
}
