using System.Buffers;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Covers the entity reuse of a lookup whose targets are matched to the producers on the raw
/// bytes of their key values: the targets are the <c>foos</c> and the producers are the
/// <c>bars</c> of one result.
/// </summary>
public sealed class FetchResultStoreEntityReuseTests : FusionTestBase
{
    private const string OperationText = "{ foos { key code flag name } bars { key code flag name } }";

    private static readonly FusionSchemaDefinition s_schema = ComposeSchema(
        """
        # name: test
        type Query {
          foos: [Foo]
          bars: [Foo]
        }

        type Foo {
          key: String
          code: Int
          flag: Boolean
          name: String
        }
        """);

    private static readonly SelectionPath s_foos = SelectionPath.Root.AppendField("foos");
    private static readonly SelectionPath s_bars = SelectionPath.Root.AppendField("bars");

    [Fact]
    public void CreateVariableValueSets_Should_FillEveryTarget_When_ProducersHoldSameKey()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a","code":1,"flag":true,"name":null},
            {"key":"b","code":2,"flag":true,"name":null},
            {"key":"a","code":3,"flag":false,"name":null}
            """,
            bars:
            """
            {"key":"b","code":7,"flag":true,"name":"bar-b"},
            {"key":"a","code":8,"flag":true,"name":"bar-a"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["key"], out var reusedCount);

        // assert
        Assert.Empty(remaining);
        Assert.Equal(2, reusedCount);
        Assert.Equal(
            """
            [{"key":"a","code":1,"flag":true,"name":"bar-a"},{"key":"b","code":2,"flag":true,"name":"bar-b"},{"key":"a","code":3,"flag":false,"name":"bar-a"}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_FillEveryTarget_When_ProducersHoldSameCompositeKey()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a","code":1,"flag":true,"name":null},
            {"key":"a","code":2,"flag":true,"name":null},
            {"key":"a","code":1,"flag":false,"name":null},
            {"key":"b","code":1,"flag":true,"name":null}
            """,
            bars:
            """
            {"key":"b","code":1,"flag":true,"name":"b-1-true"},
            {"key":"a","code":1,"flag":false,"name":"a-1-false"},
            {"key":"a","code":2,"flag":true,"name":"a-2-true"},
            {"key":"a","code":1,"flag":true,"name":"a-1-true"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["key", "code", "flag"], out var reusedCount);

        // assert
        Assert.Empty(remaining);
        Assert.Equal(4, reusedCount);
        Assert.Equal(
            """
            [{"key":"a","code":1,"flag":true,"name":"a-1-true"},{"key":"a","code":2,"flag":true,"name":"a-2-true"},{"key":"a","code":1,"flag":false,"name":"a-1-false"},{"key":"b","code":1,"flag":true,"name":"b-1-true"}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_KeepUnmatchedTarget_When_LastKeyValueOfCompositeKeyDiffers()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a","code":1,"flag":true,"name":null},
            {"key":"a","code":1,"flag":false,"name":null}
            """,
            bars:
            """
            {"key":"a","code":1,"flag":true,"name":"a-1-true"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["key", "code", "flag"], out var reusedCount);

        // assert
        Assert.Equal(1, reusedCount);
        Assert.Equal(
            """
            {"__fusion_1_key":"a","__fusion_1_code":1,"__fusion_1_flag":false}
            """,
            Assert.Single(remaining));
        Assert.Equal(
            """
            [{"key":"a","code":1,"flag":true,"name":"a-1-true"},{"key":"a","code":1,"flag":false,"name":null}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_MatchOnTokenType_When_KeyValuesHaveNoBytes()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a","code":1,"flag":true,"name":null},
            {"key":"b","code":2,"flag":false,"name":null},
            {"key":"c","code":3,"flag":true,"name":null}
            """,
            bars:
            """
            {"key":"x","code":7,"flag":false,"name":"is-false"},
            {"key":"y","code":8,"flag":true,"name":"is-true"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["flag"], out var reusedCount);

        // assert
        Assert.Empty(remaining);
        Assert.Equal(2, reusedCount);
        Assert.Equal(
            """
            [{"key":"a","code":1,"flag":true,"name":"is-true"},{"key":"b","code":2,"flag":false,"name":"is-false"},{"key":"c","code":3,"flag":true,"name":"is-true"}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_CompareRawBytes_When_KeysHoldEscapes()
    {
        // arrange
        // The first and the third target have the key of the producer in two escaped forms.
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a\"b","code":1,"flag":true,"name":null},
            {"key":"a\\b","code":2,"flag":true,"name":null},
            {"key":"a\u0022b","code":3,"flag":true,"name":null}
            """,
            bars:
            """
            {"key":"a\"b","code":7,"flag":true,"name":"quote"},
            {"key":"a\\b","code":8,"flag":true,"name":"backslash"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["key"], out var reusedCount);

        // assert
        Assert.Equal(2, reusedCount);
        Assert.Equal(
            """
            {"__fusion_1_key":"a\u0022b"}
            """,
            Assert.Single(remaining));
        Assert.Equal(
            """
            [{"key":"a\"b","code":1,"flag":true,"name":"quote"},{"key":"a\\b","code":2,"flag":true,"name":"backslash"},{"key":"a\u0022b","code":3,"flag":true,"name":null}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_SkipTarget_When_NonNullKeyIsNull()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":null,"code":1,"flag":true,"name":null},
            {"key":"a","code":2,"flag":true,"name":null}
            """,
            bars:
            """
            {"key":null,"code":7,"flag":true,"name":"no-key"},
            {"key":"a","code":8,"flag":true,"name":"bar-a"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["key"], out var reusedCount, nonNullKeys: true);

        // assert
        Assert.Empty(remaining);
        Assert.Equal(1, reusedCount);
        Assert.Equal(
            """
            [{"key":null,"code":1,"flag":true,"name":null},{"key":"a","code":2,"flag":true,"name":"bar-a"}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_KeepTarget_When_NullableKeyIsNull()
    {
        // arrange
        // A producer whose key is null does not produce.
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":null,"code":1,"flag":true,"name":null},
            {"key":"a","code":2,"flag":true,"name":null}
            """,
            bars:
            """
            {"key":null,"code":7,"flag":true,"name":"no-key"},
            {"key":"a","code":8,"flag":true,"name":"bar-a"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["key"], out var reusedCount);

        // assert
        Assert.Equal(1, reusedCount);
        Assert.Equal(
            """
            {"__fusion_1_key":null}
            """,
            Assert.Single(remaining));
        Assert.Equal(
            """
            [{"key":null,"code":1,"flag":true,"name":null},{"key":"a","code":2,"flag":true,"name":"bar-a"}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_FillFromPartlyNullKey_When_CompositeKeyHoldsOneValue()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a","code":null,"flag":true,"name":null},
            {"key":"a","code":1,"flag":true,"name":null}
            """,
            bars:
            """
            {"key":"a","code":1,"flag":true,"name":"a-1"},
            {"key":"a","code":null,"flag":true,"name":"a-null"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["key", "code"], out var reusedCount);

        // assert
        Assert.Empty(remaining);
        Assert.Equal(2, reusedCount);
        Assert.Equal(
            """
            [{"key":"a","code":null,"flag":true,"name":"a-null"},{"key":"a","code":1,"flag":true,"name":"a-1"}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_FillFromFirstProducer_When_ProducersShareAKey()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a","code":1,"flag":true,"name":null}
            """,
            bars:
            """
            {"key":"a","code":7,"flag":true,"name":"first"},
            {"key":"a","code":8,"flag":true,"name":"second"}
            """);

        // act
        var remaining = scenario.CreateVariableValueSets(["key"], out var reusedCount);

        // assert
        Assert.Empty(remaining);
        Assert.Equal(1, reusedCount);
        Assert.Equal(
            """
            [{"key":"a","code":1,"flag":true,"name":"first"}]
            """,
            scenario.RenderFoos());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void CreateVariableValueSets_Should_FillEveryTarget_When_ManyProducersHoldLongKeys(int keyCount)
    {
        // arrange
        // 500 producers with keys of about 40 bytes make the key buffer and the key table grow.
        const int count = 500;
        var foos = new StringBuilder();
        var bars = new StringBuilder();
        var expected = new StringBuilder("[");

        for (var i = 0; i < count; i++)
        {
            var separator = i == 0 ? "" : ",";
            var key = $"entity-key-with-a-rather-long-value-{i:D5}";
            var target = (i * 7) % count;
            var targetKey = $"entity-key-with-a-rather-long-value-{target:D5}";
            bars.Append($$"""{{separator}}{"key":"{{key}}","code":{{i}},"flag":{{(i % 2 == 0 ? "true" : "false")}},"name":"bar-{{i}}"}""");
            foos.Append($$"""{{separator}}{"key":"{{targetKey}}","code":{{target}},"flag":{{(target % 2 == 0 ? "true" : "false")}},"name":null}""");
            expected.Append($$"""{{separator}}{"key":"{{targetKey}}","code":{{target}},"flag":{{(target % 2 == 0 ? "true" : "false")}},"name":"bar-{{target}}"}""");
        }

        expected.Append(']');
        using var scenario = Scenario.Create(foos.ToString(), bars.ToString());
        string[] keyFields = keyCount == 1 ? ["key"] : ["key", "code", "flag"];

        // act
        var remaining = scenario.CreateVariableValueSets(keyFields, out var reusedCount);

        // assert
        Assert.Empty(remaining);
        Assert.Equal(count, reusedCount);
        Assert.Equal(expected.ToString(), scenario.RenderFoos());
    }

    [Fact]
    public void CreateVariableValueSets_Should_FillTargets_When_StoreIsReusedForSecondRequest()
    {
        // arrange
        const string foos =
            """
            {"key":"a","code":1,"flag":true,"name":null},
            {"key":"b","code":2,"flag":true,"name":null}
            """;
        const string bars =
            """
            {"key":"b","code":7,"flag":true,"name":"bar-b"},
            {"key":"a","code":8,"flag":true,"name":"bar-a"}
            """;
        using var scenario = Scenario.Create(foos, bars);
        scenario.CreateVariableValueSets(["key", "code"], out _);

        // act
        scenario.Restart(foos, bars);
        var remaining = scenario.CreateVariableValueSets(["key"], out var reusedCount);

        // assert
        Assert.Empty(remaining);
        Assert.Equal(2, reusedCount);
        Assert.Equal(
            """
            [{"key":"a","code":1,"flag":true,"name":"bar-a"},{"key":"b","code":2,"flag":true,"name":"bar-b"}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_FillEveryTarget_When_ProducersHoldSameCompositeKey()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a","code":1,"flag":true,"name":null},
            {"key":"b","code":2,"flag":true,"name":null},
            {"key":"a","code":1,"flag":false,"name":null}
            """,
            bars:
            """
            {"key":"b","code":2,"flag":true,"name":"b-2"},
            {"key":"a","code":1,"flag":true,"name":"a-1"}
            """);

        // act
        var representation = scenario.CreateRepresentation(["key", "code"], out var reusedCount);

        // assert
        Assert.True(representation.IsEmpty);
        Assert.Equal(2, reusedCount);
        Assert.Equal(
            """
            [{"key":"a","code":1,"flag":true,"name":"a-1"},{"key":"b","code":2,"flag":true,"name":"b-2"},{"key":"a","code":1,"flag":false,"name":"a-1"}]
            """,
            scenario.RenderFoos());
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SendUnmatchedEntity_When_OneTargetHasNoProducer()
    {
        // arrange
        using var scenario = Scenario.Create(
            foos:
            """
            {"key":"a","code":1,"flag":true,"name":null},
            {"key":"c","code":3,"flag":true,"name":null},
            {"key":"a","code":1,"flag":false,"name":null}
            """,
            bars:
            """
            {"key":"b","code":2,"flag":true,"name":"bar-b"},
            {"key":"a","code":1,"flag":true,"name":"bar-a"}
            """);

        // act
        var representation = scenario.CreateRepresentation(["key"], out var reusedCount);

        // assert
        Assert.Equal(2, reusedCount);
        Assert.Equal(
            """
            {"representations":[{"__typename":"Foo","key":"c"}]}
            """,
            Encoding.UTF8.GetString(representation.Value.AsSequence().ToArray()));
        Assert.Equal(
            """
            [{"key":"a","code":1,"flag":true,"name":"bar-a"},{"key":"c","code":3,"flag":true,"name":null},{"key":"a","code":1,"flag":false,"name":"bar-a"}]
            """,
            scenario.RenderFoos());
    }

    private sealed class Scenario : IDisposable
    {
        private readonly OperationPlan _plan = PlanOperation(s_schema, OperationText);
        private readonly FetchResultStore _store = new();
        private readonly List<MemoryArena> _arenas = [];

        public static Scenario Create(string foos, string bars)
        {
            var scenario = new Scenario();
            scenario.Initialize(foos, bars);
            return scenario;
        }

        /// <summary>
        /// Cleans the store as the pool does and loads a new result into it.
        /// </summary>
        public void Restart(string foos, string bars)
        {
            _store.Clean(maxCollectTargetRetainLength: 256, maxDictionaryRetainCapacity: 256);
            Initialize(foos, bars);
        }

        /// <summary>
        /// Runs the lookup of the <c>foos</c> and returns the variables of the entities that
        /// still have to be fetched.
        /// </summary>
        public List<string> CreateVariableValueSets(
            string[] keyFields,
            out int reusedCount,
            bool nonNullKeys = false)
        {
            var remaining = _store.CreateVariableValueSets(
                [s_foos],
                [],
                CreateRequirements(keyFields, nonNullKeys),
                CreateCandidates(keyFields),
                out reusedCount);

            return remaining
                .Select(t => Encoding.UTF8.GetString(t.Values.AsSequence().ToArray()))
                .ToList();
        }

        /// <summary>
        /// Runs the entity lookup of the <c>foos</c> and returns the representations of the
        /// entities that still have to be fetched.
        /// </summary>
        public RepresentationValue CreateRepresentation(string[] keyFields, out int reusedCount)
        {
            var requirements = CreateRequirements(keyFields, nonNullKeys: false);
            var arguments = string.Join(", ", keyFields.Select(t => $"{t}: $__fusion_1_{t}"));
            var document = Utf8GraphQLParser.Parse($"{{ fooByKey({arguments}) {{ name }} }}");
            var lookupField = (FieldNode)document.Definitions
                .OfType<OperationDefinitionNode>()
                .Single()
                .SelectionSet
                .Selections[0];

            return _store.CreateRepresentationVariableValue(
                [s_foos],
                [],
                requirements,
                RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo"),
                "Foo",
                CreateCandidates(keyFields),
                out reusedCount);
        }

        public string RenderFoos()
        {
            using var buffer = new PooledArrayWriter();
            _store.Result.Data.GetProperty("foos").WriteTo(buffer);
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        public void Dispose()
        {
            _store.Dispose();

            foreach (var arena in _arenas)
            {
                arena.Dispose();
            }
        }

        private void Initialize(string foos, string bars)
        {
            var resultArena = new MemoryArena();
            var sourceArena = new MemoryArena();
            _arenas.Add(resultArena);
            _arenas.Add(sourceArena);

            _store.Initialize(
                resultArena,
                s_schema,
                DefaultErrorHandler.Default,
                _plan.Operation,
                ErrorHandlingMode.Propagate,
                includeFlags: default,
                deferFlags: default,
                pathSegmentLocalPoolCapacity: 16);

            var node = Assert.IsType<OperationExecutionNode>(Assert.Single(_plan.RootNodes));
            var payload = Encoding.UTF8.GetBytes($$$"""{"data":{"foos":[{{{foos}}}],"bars":[{{{bars}}}]}}""");
            var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
            var added = _store.AddPartialResults(
                SelectionPath.Root,
                [new SourceSchemaResult(CompactPath.Root, document)],
                node.ResultSelectionSet,
                containsErrors: false);
            Assert.True(added);
        }

        private static OperationRequirement[] CreateRequirements(string[] keyFields, bool nonNullKeys)
        {
            var requirements = new OperationRequirement[keyFields.Length];

            for (var i = 0; i < keyFields.Length; i++)
            {
                ITypeNode type = new NamedTypeNode(
                    keyFields[i] switch
                    {
                        "code" => "Int",
                        "flag" => "Boolean",
                        _ => "String"
                    });

                requirements[i] = new OperationRequirement(
                    $"__fusion_1_{keyFields[i]}",
                    nonNullKeys ? new NonNullTypeNode((NamedTypeNode)type) : type,
                    SelectionPath.Root,
                    new FieldSelectionMapParser(keyFields[i]).Parse(),
                    null);
            }

            return requirements;
        }

        // Mirrors the candidate list that the plan analysis creates for a lookup of the foos
        // whose name the bars hold.
        private EntityReuseCandidateList CreateCandidates(string[] keyFields)
            => new(
                [s_bars],
                ["name"],
                keyFields,
                [CreateLayout("foos", keyFields), CreateLayout("bars", keyFields)]);

        private EntityReuseLayout CreateLayout(string rootField, string[] keyFields)
        {
            var operation = _plan.Operation;
            Assert.True(operation.RootSelectionSet.TryGetSelection(rootField, out var rootSelection));
            var selectionSet = operation.GetSelectionSet(rootSelection);

            return new EntityReuseLayout(
                selectionSet.Id,
                keyFields.Select(GetRowOffset).ToArray(),
                [GetRowOffset("name")]);

            int GetRowOffset(string responseName)
            {
                Assert.True(selectionSet.TryGetSelection(responseName, out var selection));
                return CompositeResultDocument.GetValueRowOffset(selectionSet, selection);
            }
        }
    }
}
