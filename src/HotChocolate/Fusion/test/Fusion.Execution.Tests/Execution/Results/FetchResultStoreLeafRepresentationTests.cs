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
using IntValueNode = HotChocolate.Language.IntValueNode;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Covers the representations of entity lookups whose shape consists of leaf fields. Every test
/// asserts the exact bytes of the representations variable.
/// </summary>
public sealed class FetchResultStoreLeafRepresentationTests : FusionTestBase
{
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
    public void CreateRepresentationVariableValue_Should_WriteRawLeafValues_When_KeyIsComposite()
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { key code flag } }",
            """
            {"data":{"foos":[{"key":"a","code":1,"flag":true},{"key":"b","code":-2.50e1,"flag":false}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_foos], ["key", "code", "flag"]);

        // assert
        scenario.Render(representation).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","key":"a","code":1,"flag":true},{"__typename":"Foo","key":"b","code":-2.50e1,"flag":false}]}
            foos[0]
            foos[1]
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_KeepEscapedBytes_When_KeysHoldEscapes()
    {
        // arrange
        // The first and the third entity have the same key in two escaped forms.
        using var scenario = Scenario.Create(
            "{ foos { key } }",
            """
            {"data":{"foos":[{"key":"a\"b"},{"key":"a\\b"},{"key":"a\u0022b"},{"key":"a\"b"},{"key":"é"}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_foos], ["key"]);

        // assert
        scenario.Render(representation).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","key":"a\"b"},{"__typename":"Foo","key":"a\\b"},{"__typename":"Foo","key":"a\u0022b"},{"__typename":"Foo","key":"é"}]}
            foos[0], foos[3]
            foos[1]
            foos[2]
            foos[4]
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ShareRepresentation_When_EntitiesRepeat()
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { key code } bars { code key } }",
            """
            {"data":{"foos":[{"key":"a","code":1},{"key":"a","code":2},{"key":"a","code":1}],"bars":[{"code":2,"key":"a"},{"code":3,"key":"a"},{"code":1,"key":"a"}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_foos, s_bars], ["key", "code"]);

        // assert
        scenario.Render(representation).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","key":"a","code":1},{"__typename":"Foo","key":"a","code":2},{"__typename":"Foo","key":"a","code":3}]}
            foos[0], foos[2], bars[2]
            foos[1], bars[0]
            bars[1]
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_DistinguishTokenTypes_When_RawBytesDiffer()
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { key flag } }",
            """
            {"data":{"foos":[{"key":"true","flag":true},{"key":"1","flag":false},{"key":"true","flag":true},{"key":"","flag":true}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_foos], ["key", "flag"]);

        // assert
        scenario.Render(representation).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","key":"true","flag":true},{"__typename":"Foo","key":"1","flag":false},{"__typename":"Foo","key":"","flag":true}]}
            foos[0], foos[2]
            foos[1]
            foos[3]
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_WriteNull_When_NullableKeyIsNull()
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { key code } }",
            """
            {"data":{"foos":[{"key":"a","code":1},{"key":null,"code":1},{"key":"a","code":null},{"key":null,"code":1},{"key":"a","code":1}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_foos], ["key", "code"]);

        // assert
        scenario.Render(representation).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","key":"a","code":1},{"__typename":"Foo","key":null,"code":1},{"__typename":"Foo","key":"a","code":null}]}
            foos[0], foos[4]
            foos[1], foos[3]
            foos[2]
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SkipEntity_When_NonNullKeyIsNull()
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { key code } }",
            """
            {"data":{"foos":[{"key":null,"code":1},{"key":"a","code":1},{"key":"b","code":null}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_foos], ["key", "code"], nonNullKeys: true);

        // assert
        scenario.Render(representation).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","key":"a","code":1}]}
            foos[1]
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SkipEntities_When_SelectionSetLacksAKeyField()
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { key code } bars { key } }",
            """
            {"data":{"foos":[{"key":"a","code":1}],"bars":[{"key":"a"},{"key":"b"}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_bars, s_foos], ["key", "code"]);

        // assert
        scenario.Render(representation).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","key":"a","code":1}]}
            foos[0]
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ReadAliasedField_When_ResponseNameDiffers()
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { id: key code } }",
            """
            {"data":{"foos":[{"id":"a","code":1},{"id":"b","code":2}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_foos], ["key", "code"], keyAlias: "id");

        // assert
        scenario.Render(representation).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","key":"a","code":1},{"__typename":"Foo","key":"b","code":2}]}
            foos[0]
            foos[1]
            """);
    }

    [Theory]
    [InlineData("Foo", "\"Foo\"")]
    [InlineData("Foö", "\"Fo\\u00F6\"")]
    [InlineData(
        "ATypeNameThatIsLongerThanTheTypeNamesThatTheRepresentationsOfALookupAreUsuallyWrittenForWhichTakesOneHundredAndTwentyNineCharacters",
        "\"ATypeNameThatIsLongerThanTheTypeNamesThatTheRepresentationsOfALookupAreUsuallyWrittenForWhichTakesOneHundredAndTwentyNineCharacters\"")]
    public void CreateRepresentationVariableValue_Should_WriteTypeNameAsJsonString_When_TypeNameVaries(
        string entityTypeName,
        string expectedTypeName)
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { key } }",
            """
            {"data":{"foos":[{"key":"a"},{"key":"b"},{"key":"a"}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation([s_foos], ["key"], entityTypeName: entityTypeName);

        // assert
        Assert.Equal(
            $$"""{"representations":[{"__typename":{{expectedTypeName}},"key":"a"},{"__typename":{{expectedTypeName}},"key":"b"}]}""",
            Encoding.UTF8.GetString(representation.Value.AsSequence().ToArray()));
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_AppendRequestVariables_When_VariablesAreForwarded()
    {
        // arrange
        using var scenario = Scenario.Create(
            "{ foos { key } }",
            """
            {"data":{"foos":[{"key":"a"},{"key":"b"}]}}
            """);

        // act
        var representation = scenario.CreateRepresentation(
            [s_foos],
            ["key"],
            requestVariables: [new ObjectFieldNode("first", new IntValueNode(5))]);

        // assert
        Assert.Equal(
            """{"representations":[{"__typename":"Foo","key":"a"},{"__typename":"Foo","key":"b"}],"first":5}""",
            Encoding.UTF8.GetString(representation.Value.AsSequence().ToArray()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void CreateRepresentationVariableValue_Should_WriteEntitiesInOrder_When_ManyEntitiesRepeat(int keyCount)
    {
        // arrange
        // 5000 distinct entities of about 80 bytes fill more than one chunk of the buffer that
        // holds the representations. Every entity occurs a second time after all first
        // occurrences; a repeated entity that is not recognized is written again at the end.
        const int count = 5000;
        var payload = new StringBuilder("""{"data":{"foos":[""");
        var expected = new StringBuilder("""{"representations":[""");

        for (var i = 0; i < count * 2; i++)
        {
            var id = i % count;
            var separator = i == 0 ? "" : ",";
            var key = $"entity-key-with-a-rather-long-value-{id:D5}";
            var flag = id % 2 == 0 ? "true" : "false";
            payload.Append($$"""{{separator}}{"key":"{{key}}","code":{{id}},"flag":{{flag}}}""");

            if (i < count)
            {
                expected.Append(
                    keyCount == 1
                        ? $$"""{{separator}}{"__typename":"Foo","key":"{{key}}"}"""
                        : $$"""{{separator}}{"__typename":"Foo","key":"{{key}}","code":{{id}},"flag":{{flag}}}""");
            }
        }

        payload.Append("]}}");
        using var scenario = Scenario.Create("{ foos { key code flag } }", payload.ToString());
        string[] keyFields = keyCount == 1 ? ["key"] : ["key", "code", "flag"];

        // act
        var representation = scenario.CreateRepresentation([s_foos], keyFields);

        // assert
        var actual = Encoding.UTF8.GetString(representation.Value.AsSequence().ToArray());
        Assert.StartsWith(expected.ToString(), actual);
        Assert.EndsWith("}]}", actual);
        Assert.Equal(
            count * 2,
            representation.ResultPaths.Sum(t => 1 + t.AdditionalPaths.Length));
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_WriteSameBytes_When_StoreIsReusedForSecondRequest()
    {
        // arrange
        const string payload =
            """
            {"data":{"foos":[{"key":"a","code":1},{"key":"b","code":2},{"key":"a","code":1}]}}
            """;
        using var scenario = Scenario.Create("{ foos { key code } }", payload);
        var first = scenario.Render(scenario.CreateRepresentation([s_foos], ["key", "code"]));

        // act
        scenario.Restart(payload);
        var second = scenario.Render(scenario.CreateRepresentation([s_foos], ["key"]));

        // assert
        Assert.Equal(
            """
            {"representations":[{"__typename":"Foo","key":"a","code":1},{"__typename":"Foo","key":"b","code":2}]}
            foos[0], foos[2]
            foos[1]
            """,
            first);
        Assert.Equal(
            """
            {"representations":[{"__typename":"Foo","key":"a"},{"__typename":"Foo","key":"b"}]}
            foos[0], foos[2]
            foos[1]
            """,
            second);
    }

    private sealed class Scenario : IDisposable
    {
        private readonly OperationPlan _plan;
        private readonly FetchResultStore _store = new();
        private readonly List<MemoryArena> _arenas = [];

        private Scenario(string operationText)
        {
            _plan = PlanOperation(s_schema, operationText);
        }

        public static Scenario Create(string operationText, string payload)
        {
            var scenario = new Scenario(operationText);
            scenario.Initialize(payload);
            return scenario;
        }

        /// <summary>
        /// Cleans the store as the pool does and loads a new result into it.
        /// </summary>
        public void Restart(string payload)
        {
            _store.Clean(maxCollectTargetRetainLength: 256, maxDictionaryRetainCapacity: 256);
            Initialize(payload);
        }

        /// <summary>
        /// Creates the representations of the entities at <paramref name="targets"/> for a
        /// lookup that takes <paramref name="keyFields"/>.
        /// </summary>
        public RepresentationValue CreateRepresentation(
            SelectionPath[] targets,
            string[] keyFields,
            bool nonNullKeys = false,
            string? keyAlias = null,
            string entityTypeName = "Foo",
            ObjectFieldNode[]? requestVariables = null)
        {
            var requirements = new OperationRequirement[keyFields.Length];

            for (var i = 0; i < keyFields.Length; i++)
            {
                var type = new NamedTypeNode(
                    keyFields[i] switch
                    {
                        "code" => "Int",
                        "flag" => "Boolean",
                        _ => "String"
                    });

                requirements[i] = new OperationRequirement(
                    $"__fusion_1_{keyFields[i]}",
                    nonNullKeys ? new NonNullTypeNode(type) : type,
                    SelectionPath.Root,
                    new FieldSelectionMapParser(keyFields[i]).Parse(),
                    keyFields[i] is "key" ? keyAlias : null);
            }

            var arguments = string.Join(", ", keyFields.Select(t => $"{t}: $__fusion_1_{t}"));
            var document = Utf8GraphQLParser.Parse($"{{ fooByKey({arguments}) {{ name }} }}");
            var lookupField = (FieldNode)document.Definitions
                .OfType<OperationDefinitionNode>()
                .Single()
                .SelectionSet
                .Selections[0];

            return _store.CreateRepresentationVariableValue(
                targets,
                requestVariables ?? [],
                requirements,
                RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo"),
                entityTypeName);
        }

        /// <summary>
        /// Renders the bytes of the representations variable, followed by one line per
        /// representation with the paths of the entities it stands for.
        /// </summary>
        public string Render(RepresentationValue representation)
        {
            var operation = _store.Result.Data.Operation;
            var output = new StringBuilder();
            output.Append(Encoding.UTF8.GetString(representation.Value.AsSequence().ToArray()));

            foreach (var resultPath in representation.ResultPaths)
            {
                output.AppendLine();
                output.Append(resultPath.Path.ToPath(operation).Print());

                foreach (var additionalPath in resultPath.AdditionalPaths)
                {
                    output.Append(", ");
                    output.Append(additionalPath.ToPath(operation).Print());
                }
            }

            return output.ToString();
        }

        public void Dispose()
        {
            _store.Dispose();

            foreach (var arena in _arenas)
            {
                arena.Dispose();
            }
        }

        private void Initialize(string payload)
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
            var bytes = Encoding.UTF8.GetBytes(payload);
            var document = SourceResultDocument.Parse(sourceArena, bytes, bytes.Length);
            var added = _store.AddPartialResults(
                SelectionPath.Root,
                [new SourceSchemaResult(CompactPath.Root, document)],
                node.ResultSelectionSet,
                containsErrors: false);
            Assert.True(added);
        }
    }
}
