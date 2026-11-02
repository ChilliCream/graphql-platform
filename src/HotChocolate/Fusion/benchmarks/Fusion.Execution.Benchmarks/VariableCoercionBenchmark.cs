using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures coercion of an input object variable through
/// <see cref="VariableCoercionHelper.TryCoerceVariableValues"/> for a small input
/// (2 input objects, 5 fields) and a large input holding a list of 20 input objects
/// (22 input objects, 66 fields). Both inputs contain an enum value.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class VariableCoercionBenchmark
{
    /// <summary>
    /// BenchmarkDotNet has no RuntimeMoniker for the net11.0 preview host this project
    /// targets, so the job runs in process with 3 warmup and 10 measurement iterations.
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

    private const int LargeInputItemCount = 20;

    private static readonly IFeatureProvider s_featureProvider = new BenchmarkFeatureProvider();

    private FusionSchemaDefinition _schema = null!;
    private VariableDefinitionNode[] _variableDefinitions = null!;
    private JsonDocument _smallInput = null!;
    private JsonDocument _largeInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        _schema = ComposeSchema();
        _variableDefinitions =
        [
            new VariableDefinitionNode(
                null,
                new VariableNode("input"),
                description: null,
                new NonNullTypeNode(new NamedTypeNode("SearchInput")),
                null,
                Array.Empty<DirectiveNode>())
        ];
        _smallInput = JsonDocument.Parse(
            """
            {"input":{"query":"shoes","filter":{"category":"footwear","minPrice":10.5,"sort":"ASC"}}}
            """);
        _largeInput = JsonDocument.Parse(BuildLargeInput());

        if (Coerce(_smallInput) is null || Coerce(_largeInput) is null)
        {
            throw new InvalidOperationException("The benchmark inputs do not coerce.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _smallInput.Dispose();
        _largeInput.Dispose();
    }

    [Benchmark]
    public Dictionary<string, VariableValue>? SmallInput()
        => Coerce(_smallInput);

    [Benchmark]
    public Dictionary<string, VariableValue>? LargeInput()
        => Coerce(_largeInput);

    private Dictionary<string, VariableValue>? Coerce(JsonDocument input)
    {
        VariableCoercionHelper.TryCoerceVariableValues(
            s_featureProvider,
            _schema,
            _variableDefinitions,
            input.RootElement,
            ignoreAdditionalInputFields: false,
            out var coercedVariableValues,
            out _);

        return coercedVariableValues;
    }

    private static string BuildLargeInput()
    {
        var builder = new StringBuilder();
        builder.Append(
            """
            {"input":{"query":"shoes","filter":{"category":"footwear","minPrice":10.5,"sort":"DESC"},"items":[
            """);

        for (var i = 0; i < LargeInputItemCount; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append($$"""{"id":"item-{{i}}","quantity":{{i + 1}},"note":"note {{i}}"}""");
        }

        builder.Append("]}}");
        return builder.ToString();
    }

    private static FusionSchemaDefinition ComposeSchema()
    {
        List<SourceSchemaText> sourceSchemas =
        [
            new SourceSchemaText(
                "search",
                """
                type Query {
                  search(input: SearchInput!): String
                }

                input SearchInput {
                  query: String!
                  filter: FilterInput
                  items: [ItemInput!]
                }

                input FilterInput {
                  category: String
                  minPrice: Float
                  maxPrice: Float
                  sort: SortOrder
                }

                input ItemInput {
                  id: ID!
                  quantity: Int!
                  note: String
                }

                enum SortOrder {
                  ASC
                  DESC
                }
                """)
        ];

        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(sourceSchemas, new SchemaComposerOptions(), compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private sealed class BenchmarkFeatureProvider : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
