using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using IntValueNode = HotChocolate.Language.IntValueNode;
using NameNode = HotChocolate.Language.NameNode;
using NullValueNode = HotChocolate.Language.NullValueNode;
using StringValueNode = HotChocolate.Language.StringValueNode;
using ListValueNode = HotChocolate.Language.ListValueNode;
using ObjectValueNode = HotChocolate.Language.ObjectValueNode;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;
using IValueNode = HotChocolate.Language.IValueNode;

namespace HotChocolate.Fusion.Execution.Results;

public sealed class FetchResultStoreRepresentationTests : FusionTestBase
{
    private static readonly FusionSchemaDefinition s_schema = ComposeSchema(
        """
        # name: test
        type Query {
          noop: String
        }
        """);

    private static readonly Operation s_operation = PlanOperation(s_schema, "{ noop }").Operation;

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_NestRequireUnderStructuralField_When_RequireArgIsOnNestedField()
    {
        // arrange
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id) { bar { x(y: $__fusion_2_y) } } }");
        var entry = CreateEntry(
            source,
            CompactPath.Root,
            Field("__fusion_1_id", new IntValueNode(1)),
            Field("__fusion_2_y", new StringValueNode("v")));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id", "__fusion_2_y"),
            [],
            [Requirement("__fusion_1_id", "id"), Requirement("__fusion_2_y", "y")],
            "Foo",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":1,"bar":{"y":"v"}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_MergeBothKeysAtRoot_When_CompositeKey()
    {
        // arrange
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField(
            "{ productBySkuPackage(sku: $__fusion_1_sku, package: $__fusion_1_package) { id } }");
        var entry = CreateEntry(
            source,
            CompactPath.Root,
            Field("__fusion_1_sku", new StringValueNode("s")),
            Field("__fusion_1_package", new StringValueNode("p")));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_sku", "__fusion_1_package"),
            [],
            [Requirement("__fusion_1_sku", "sku"), Requirement("__fusion_1_package", "package")],
            "Product",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Product","sku":"s","package":"p"}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_ProjectRhsShape_When_RequireMapUsesAliasedPath()
    {
        // arrange
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField("{ productByPKey(pkey: $__fusion_1_pkey) { id } }");
        var entry = CreateEntry(
            source,
            CompactPath.Root,
            Field(
                "__fusion_1_pkey",
                new ObjectValueNode(
                    Field("name", new StringValueNode("n")),
                    Field("brandName", new StringValueNode("b")))));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_pkey"),
            [],
            [Requirement("__fusion_1_pkey", "{ name brandName: brand.name }")],
            "Product",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Product","name":"n","brand":{"name":"b"}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_EmitSourceShapeAtRoot_When_KeyMapIsObjectValueSelection()
    {
        // arrange
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField("{ thingByCompositeId(key: $__fusion_1_key) { id } }");
        var entry = CreateEntry(
            source,
            CompactPath.Root,
            Field(
                "__fusion_1_key",
                new ObjectValueNode(
                    Field("two", new IntValueNode(2)),
                    Field("three", new IntValueNode(3)))));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_key"),
            [],
            [Requirement("__fusion_1_key", "{ two: compositeId.two three: compositeId.three }")],
            "Thing",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Thing","compositeId":{"two":2,"three":3}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_ProjectListElements_When_RequireMapIsListSelection()
    {
        // arrange
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField(
            "{ productById(id: $__fusion_1_id, similar: $__fusion_2_similar) { id } }");
        var entry = CreateEntry(
            source,
            CompactPath.Root,
            Field("__fusion_1_id", new IntValueNode(1)),
            Field(
                "__fusion_2_similar",
                new ListValueNode(
                    new ObjectValueNode(Field("id", new IntValueNode(2))),
                    new ObjectValueNode(Field("id", new IntValueNode(3))))));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id", "__fusion_2_similar"),
            [],
            [Requirement("__fusion_1_id", "id"), Requirement("__fusion_2_similar", "similar[{ id: id }]")],
            "Product",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Product","id":1,"similar":[{"id":2},{"id":3}]}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_DeduplicateRepresentations_When_EntriesProduceIdenticalValues()
    {
        // arrange
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var firstPath = Path(1);
        var secondPath = Path(2);
        var lookupField = ParseLookupField("{ fooById(id: $__fusion_1_id) { id } }");
        var first = CreateEntry(source, firstPath, Field("__fusion_1_id", new IntValueNode(1)));
        var second = CreateEntry(source, secondPath, Field("__fusion_1_id", new IntValueNode(1)));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [first, second],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id", "id")],
            "Foo",
            lookupField);

        // assert
        var resultPath = Assert.Single(representation.ResultPaths);
        Assert.Equal(firstPath, resultPath.Path);
        Assert.Equal([secondPath], resultPath.AdditionalPaths.AsSpan().ToArray());
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":1}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_ReturnEmpty_When_AllEntriesAreEmpty()
    {
        // arrange
        using var arena = new MemoryArena();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField("{ fooById(id: $__fusion_1_id) { id } }");

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [VariableValues.Empty],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id", "id")],
            "Foo",
            lookupField);

        // assert
        Assert.True(representation.IsEmpty);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_Throw_When_RequirementWasNotImported()
    {
        // arrange
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField("{ fooById(id: $__fusion_1_id) { id } }");
        var entry = CreateEntry(source, CompactPath.Root, Field("__fusion_1_id", new IntValueNode(1)));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateRepresentationFromSnapshot(
                target,
                s_schema,
                [entry],
                ImportedKeys("__fusion_2_sku"),
                [],
                [Requirement("__fusion_1_id", "id")],
                "Foo",
                lookupField));

        // assert
        Assert.Equal(
            "A deferred incremental plan fetch references a requirement that was not imported.",
            exception.Message);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_NestedRequire()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              bar: Bar
            }

            type Bar {
              y: String
            }
            """);
        var plan = PlanOperation(schema, "{ foos { id bar { y } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id) { bar { x(y: $__fusion_2_y) } } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement("__fusion_2_y", "y")
        };

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var payload = """{"data":{"foos":[{"id":"1","bar":{"y":"v"}}]}}"""u8.ToArray();
        var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            node.ResultSelectionSet,
            containsErrors: false);

        using var snapshotArena = new MemoryArena();
        using var snapshotSource = new FetchResultStore();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);
        var entry = CreateEntry(
            snapshotSource,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")),
            Field("__fusion_2_y", new StringValueNode("v")));

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id", "__fusion_2_y"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.True(added);
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"1","bar":{"y":"v"}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ReadInternalAlias_When_RequirementHasInternalAlias()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
            }
            """);
        var plan = PlanOperation(schema, "{ foos { __fusion_internal_id: id } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var lookupField = ParseLookupField("{ fooById(id: $__fusion_1_id) { id } }");
        var requirement = new OperationRequirement(
            "__fusion_1_id",
            new NamedTypeNode("String"),
            SelectionPath.Root,
            new FieldSelectionMapParser("id").Parse(),
            "__fusion_internal_id");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var payload = """{"data":{"foos":[{"__fusion_internal_id":"1"}]}}"""u8.ToArray();
        var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            node.ResultSelectionSet,
            containsErrors: false);

        // act
        var representation = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            [requirement],
            "Foo",
            lookupField);

        // assert
        Assert.True(added);
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"1"}]}
            """);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_MatchValueSelectionRewriterOutput_When_MapsAreRewritten()
    {
        // arrange
        var lookupField = ParseLookupField(
            "{ foo(a: $__fusion_1_a, b: $__fusion_2_b, c: $__fusion_3_c) { id } }");
        var maps = new IValueSelectionNode[]
        {
            new FieldSelectionMapParser("brand.name").Parse(),
            new FieldSelectionMapParser("{ name brandName: brand.name }").Parse(),
            new FieldSelectionMapParser("dimensions.{ size weight }").Parse()
        };
        var stringType = new NamedTypeNode("String");
        var requirements = new[]
        {
            new OperationRequirement("__fusion_1_a", stringType, SelectionPath.Root, maps[0], null),
            new OperationRequirement("__fusion_2_b", stringType, SelectionPath.Root, maps[1], null),
            new OperationRequirement("__fusion_3_c", stringType, SelectionPath.Root, maps[2], null)
        };

        // act
        var shape = RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo");

        // assert
        // The shape merges overlapping source paths; the rewriter output is the
        // unmerged semantic oracle for the source shape of each map.
        RenderShape(shape).ToString(indented: true).MatchInlineSnapshot(
            """
            {
              brand {
                name
              }
              name
              dimensions {
                size
                weight
              }
            }
            """);
        ValueSelectionToSelectionSetRewriter.Rewrite(maps).ToString(indented: true).MatchInlineSnapshot(
            """
            {
              brand {
                name
              }
              name
              brand {
                name
              }
              dimensions {
                ... {
                  size
                  weight
                }
              }
            }
            """);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_SetParentTypeCondition_When_MapIsTypeConditioned()
    {
        // arrange
        var lookupField = ParseLookupField("{ fooById(id: $__fusion_1_id) { id } }");
        var requirements = new[] { Requirement("__fusion_1_id", "<Bar>.id") };

        // act
        var shape = RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo");

        // assert
        var node = Assert.Single(shape);
        Assert.Equal("id", node.Name);
        Assert.Equal("Bar", node.ParentTypeCondition);
        Assert.Null(node.Children);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_CreateCoexistingBranches_When_MapMixesUnconditionalAndTypeConditionedPaths()
    {
        // arrange
        var lookupField = ParseLookupField("{ entityById(input: $__fusion_1_input) { id } }");
        var requirements = new[]
        {
            Requirement(
                "__fusion_1_input",
                "{ foo: data.foo bar: data<Bar>.bar qux: data<Qux>.qux }")
        };

        // act
        var shape = RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo");

        // assert
        Assert.Collection(
            shape,
            node =>
            {
                node.Name.MatchInlineSnapshot(
                    """
                    data
                    """);
                Assert.Collection(
                    node.Children!,
                    child => $"{child.Name}:{child.Children is null}".MatchInlineSnapshot(
                        """
                        foo:True
                        """));
                Assert.Collection(
                    node.Branches!,
                    branch =>
                    {
                        branch.TypeCondition.MatchInlineSnapshot(
                            """
                            Bar
                            """);
                        Assert.Collection(
                            branch.Children,
                            child => $"{child.Name}:{child.Children is null}".MatchInlineSnapshot(
                                """
                                bar:True
                                """));
                    },
                    branch =>
                    {
                        branch.TypeCondition.MatchInlineSnapshot(
                            """
                            Qux
                            """);
                        Assert.Collection(
                            branch.Children,
                            child => $"{child.Name}:{child.Children is null}".MatchInlineSnapshot(
                                """
                                qux:True
                                """));
                    });
            });
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_MergeIntoSameBranch_When_TwoPathsShareTypeCondition()
    {
        // arrange
        var lookupField = ParseLookupField("{ entityById(input: $__fusion_1_input) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_input", "{ bar: data<Bar>.bar baz: data<Bar>.baz }")
        };

        // act
        var shape = RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo");

        // assert
        Assert.Collection(
            shape,
            node =>
            {
                node.Name.MatchInlineSnapshot(
                    """
                    data
                    """);
                Assert.Empty(node.Children!);
                Assert.Collection(
                    node.Branches!,
                    branch =>
                    {
                        branch.TypeCondition.MatchInlineSnapshot(
                            """
                            Bar
                            """);
                        Assert.Collection(
                            branch.Children,
                            child => $"{child.Name}:{child.Children is null}".MatchInlineSnapshot(
                                """
                                bar:True
                                """),
                            child => $"{child.Name}:{child.Children is null}".MatchInlineSnapshot(
                                """
                                baz:True
                                """));
                    });
            });
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_Throw_When_LeafConflictsWithTypeConditionedComposite()
    {
        // arrange
        var lookupField = ParseLookupField("{ e(a: $__fusion_1_data, b: $__fusion_2_data) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_data", "data"),
            Requirement("__fusion_2_data", "{ bar: data<Bar>.bar }")
        };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo"));

        // assert
        Assert.Equal(
            "The requirement maps produce conflicting representation nodes for 'data'.",
            exception.Message);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_Throw_When_RequirementIsNotBound()
    {
        // arrange
        var lookupField = ParseLookupField("{ fooById(id: $other) { id } }");
        var requirements = new[] { Requirement("__fusion_1_id", "id") };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo"));

        // assert
        Assert.Equal(
            "The lookup selection does not bind the requirement '__fusion_1_id' to an argument.",
            exception.Message);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_Throw_When_RequirementIsBoundTwice()
    {
        // arrange
        var lookupField = ParseLookupField(
            "{ foo(a: $__fusion_1_id, b: $__fusion_1_id) { id } }");
        var requirements = new[] { Requirement("__fusion_1_id", "id") };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo"));

        // assert
        Assert.Equal(
            "The lookup selection binds the requirement '__fusion_1_id' to more than one argument.",
            exception.Message);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_Throw_When_LeafAndStructuralNodesConflict()
    {
        // arrange
        // The root key map claims 'bar' as a leaf while the nested requirement
        // needs 'bar' as a structural parent.
        var lookupField = ParseLookupField(
            "{ foo(a: $__fusion_1_bar) { bar { x(y: $__fusion_2_y) } } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_bar", "bar"),
            Requirement("__fusion_2_y", "y")
        };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo"));

        // assert
        Assert.Equal(
            "The requirement maps produce conflicting representation nodes for 'bar'.",
            exception.Message);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_Throw_When_ResponseNamesConflict()
    {
        // arrange
        var lookupField = ParseLookupField(
            "{ foo { bar { x(y: $__fusion_1_y) } b: bar { w(z: $__fusion_2_z) } } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_y", "y"),
            Requirement("__fusion_2_z", "z")
        };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => RepresentationShapeBuilder.Build(lookupField, requirements, s_schema, "Foo"));

        // assert
        Assert.Equal(
            "The lookup selection resolves the representation node 'bar' "
            + "under conflicting response names.",
            exception.Message);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_EmitValue_When_MapIsTypeConditioned()
    {
        // arrange
        // The type condition is applied when the requirement value is extracted;
        // the snapshot path emits the already extracted value.
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField("{ fooById(id: $__fusion_1_id) { id } }");
        var entry = CreateEntry(source, CompactPath.Root, Field("__fusion_1_id", new IntValueNode(1)));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id", "<Foo>.id")],
            "Foo",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":1}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_MergeRhsPrefix_When_RequirementsSharePathPrefix()
    {
        // arrange
        // Both maps project into 'y'; each leaf is sliced from its own requirement.
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField("{ foo(a: $__fusion_1_a, b: $__fusion_2_b) { id } }");
        var entry = CreateEntry(
            source,
            CompactPath.Root,
            Field("__fusion_1_a", new ObjectValueNode(Field("z", new IntValueNode(1)))),
            Field("__fusion_2_b", new ObjectValueNode(Field("w", new IntValueNode(2)))));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_a", "__fusion_2_b"),
            [],
            [Requirement("__fusion_1_a", "{ z: y.z }"), Requirement("__fusion_2_b", "{ w: y.w }")],
            "Foo",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","y":{"z":1,"w":2}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_ProjectSourceFieldName_When_ListElementMapIsAliased()
    {
        // arrange
        // The element map reads the input property 'pid' and emits the source
        // field 'id'.
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField(
            "{ productById(id: $__fusion_1_id, similar: $__fusion_2_similar) { id } }");
        var entry = CreateEntry(
            source,
            CompactPath.Root,
            Field("__fusion_1_id", new IntValueNode(1)),
            Field(
                "__fusion_2_similar",
                new ListValueNode(
                    new ObjectValueNode(Field("pid", new IntValueNode(2))),
                    new ObjectValueNode(Field("pid", new IntValueNode(3))))));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id", "__fusion_2_similar"),
            [],
            [Requirement("__fusion_1_id", "id"), Requirement("__fusion_2_similar", "similar[{ pid: id }]")],
            "Product",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Product","id":1,"similar":[{"id":2},{"id":3}]}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValueFromSnapshot_Should_ProjectCompositeElementShape_When_ListElementMapUsesPath()
    {
        // arrange
        using var arena = new MemoryArena();
        using var source = new FetchResultStore();
        using var target = CreateSnapshotStore(arena);

        var lookupField = ParseLookupField(
            "{ productById(id: $__fusion_1_id, similar: $__fusion_2_similar) { id } }");
        var entry = CreateEntry(
            source,
            CompactPath.Root,
            Field("__fusion_1_id", new IntValueNode(1)),
            Field(
                "__fusion_2_similar",
                new ListValueNode(
                    new ObjectValueNode(Field("name", new StringValueNode("acme"))),
                    new ObjectValueNode(Field("name", new StringValueNode("zeta"))))));

        // act
        var representation = CreateRepresentationFromSnapshot(
            target,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id", "__fusion_2_similar"),
            [],
            [
                Requirement("__fusion_1_id", "id"),
                Requirement("__fusion_2_similar", "similar[{ name: brand.name }]")
            ],
            "Product",
            lookupField);

        // assert
        Normalize(representation.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Product","id":1,"similar":[{"brand":{"name":"acme"}},{"brand":{"name":"zeta"}}]}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SkipEntity_When_TypeConditionDoesNotMatch()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
              qux: Qux
            }

            type Foo {
              id: ID!
            }

            type Qux {
              id: ID!
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id } }",
            """{"data":{"foos":[{"id":"1"}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField("{ fooById(id: $__fusion_1_id) { id } }");

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            [Requirement("__fusion_1_id", "<Qux>.id")],
            "Foo",
            lookupField);

        // assert
        Assert.True(live.IsEmpty);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_MatchSnapshotBytes_When_TypeConditionMatches()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
              qux: Qux
            }

            type Foo {
              id: ID!
            }

            type Qux {
              id: ID!
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id } }",
            """{"data":{"foos":[{"id":"1"}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField("{ fooById(id: $__fusion_1_id) { id } }");
        var requirements = new[] { Requirement("__fusion_1_id", "<Foo>.id") };

        using var snapshotArena = new MemoryArena();
        using var snapshotSource = new FetchResultStore();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);
        var entry = CreateEntry(
            snapshotSource,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")));

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"1"}]}
            """);
    }

    private const string AbstractBranchSchema =
        """
        # name: test
        type Query {
          entities: [Entity]
        }

        type Entity {
          id: ID!
          data: Foo
        }

        interface Foo {
          foo: String
        }

        interface Bar implements Foo {
          foo: String
          bar: String
        }

        type Qux implements Foo & Bar {
          foo: String
          bar: String
          qux: String
        }

        type Baz implements Foo & Bar {
          foo: String
          bar: String
          baz: String
        }
        """;

    [Fact]
    public void CreateRepresentationVariableValue_Should_EmitTypenameAndMatchingBranches_When_RuntimeTypeImplementsBranch()
    {
        // arrange
        var schema = ComposeSchema(AbstractBranchSchema);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ entities { id data { __typename foo ... on Bar { bar } ... on Baz { baz } ... on Qux { qux } } } }",
            """{"data":{"entities":[{"id":"1","data":{"__typename":"Qux","foo":"f","bar":"b","qux":"q"}}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField("{ entityById(input: $__fusion_1_input) { id } }");
        var requirements = new[]
        {
            Requirement(
                "__fusion_1_input",
                "{ foo: data.foo bar: data<Bar>.bar baz: data<Baz>.baz qux: data<Qux>.qux }")
        };

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("entities"),
            [],
            requirements,
            "Entity",
            lookupField);

        // assert
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Entity","data":{"__typename":"Qux","foo":"f","bar":"b","qux":"q"}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_OmitNonMatchingBranch_When_RuntimeTypeIsDifferentImplementor()
    {
        // arrange
        var schema = ComposeSchema(AbstractBranchSchema);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ entities { id data { __typename foo ... on Bar { bar } ... on Baz { baz } ... on Qux { qux } } } }",
            """{"data":{"entities":[{"id":"1","data":{"__typename":"Baz","foo":"f","bar":"b","baz":"z"}}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField("{ entityById(input: $__fusion_1_input) { id } }");
        var requirements = new[]
        {
            Requirement(
                "__fusion_1_input",
                "{ foo: data.foo bar: data<Bar>.bar baz: data<Baz>.baz qux: data<Qux>.qux }")
        };

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("entities"),
            [],
            requirements,
            "Entity",
            lookupField);

        // assert
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Entity","data":{"__typename":"Baz","foo":"f","bar":"b","baz":"z"}}]}
            """);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_SetRequiresTypeName_When_CompositeFieldIsAbstractAndUnbranched()
    {
        // arrange
        // 'Entity.data' is an interface, so the unbranched composite must carry
        // the runtime __typename for the source schema to reconstruct the value.
        var schema = ComposeSchema(AbstractBranchSchema);
        var lookupField = ParseLookupField("{ entityById(input: $__fusion_1_input) { id } }");
        var requirements = new[] { Requirement("__fusion_1_input", "{ foo: data.foo }") };

        // act
        var shape = RepresentationShapeBuilder.Build(lookupField, requirements, schema, "Entity");

        // assert
        var node = Assert.Single(shape);
        Assert.Equal("data", node.Name);
        Assert.Null(node.Branches);
        Assert.True(node.RequiresTypeName);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_NotSetRequiresTypeName_When_CompositeFieldIsBranched()
    {
        // arrange
        // A branched composite already emits __typename via its branch handling,
        // so the unbranched typename flag must stay clear.
        var schema = ComposeSchema(AbstractBranchSchema);
        var lookupField = ParseLookupField("{ entityById(input: $__fusion_1_input) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_input", "{ foo: data.foo bar: data<Bar>.bar }")
        };

        // act
        var shape = RepresentationShapeBuilder.Build(lookupField, requirements, schema, "Entity");

        // assert
        var node = Assert.Single(shape);
        Assert.Equal("data", node.Name);
        Assert.NotNull(node.Branches);
        Assert.False(node.RequiresTypeName);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_EmitTypename_When_CompositeFieldIsAbstractAndUnbranched()
    {
        // arrange
        // 'Entity.data' is an interface; the unbranched requirement 'data.foo'
        // must ship the runtime __typename so the source schema can reconstruct
        // the abstract value.
        var schema = ComposeSchema(AbstractBranchSchema);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ entities { id data { __typename foo } } }",
            """{"data":{"entities":[{"id":"1","data":{"__typename":"Qux","foo":"f"}}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField("{ entityById(input: $__fusion_1_input) { id } }");
        var requirements = new[] { Requirement("__fusion_1_input", "{ foo: data.foo }") };

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("entities"),
            [],
            requirements,
            "Entity",
            lookupField);

        // assert
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Entity","data":{"__typename":"Qux","foo":"f"}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SkipEntity_When_StructuralFieldIsNull()
    {
        // arrange
        // The nested requirement is unresolvable when 'bar' is null; the snapshot
        // entry equivalently lacks the nested requirement key.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              bar: Bar
            }

            type Bar {
              y: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id bar { y } } }",
            """{"data":{"foos":[{"id":"1","bar":null}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id) { bar { x(y: $__fusion_2_y) } } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement("__fusion_2_y", "y")
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotSource = new FetchResultStore();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);
        var entry = CreateEntry(
            snapshotSource,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")));

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id", "__fusion_2_y"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.True(live.IsEmpty);
        Assert.True(snapshot.IsEmpty);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_EmitStructureWithNullLeaves_When_PathIntermediateIsNull()
    {
        // arrange
        // A null intermediate of a plain path resolves the leaf to null; the
        // extracted requirement value is null on the snapshot side.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              brand: Brand
            }

            type Brand {
              name: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id brand { name } } }",
            """{"data":{"foos":[{"id":"1","brand":null}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, bn: $__fusion_2_bn) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement("__fusion_2_bn", "brand.name")
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotSource = new FetchResultStore();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);
        var entry = CreateEntry(
            snapshotSource,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")),
            Field("__fusion_2_bn", NullValueNode.Default));

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id", "__fusion_2_bn"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"1","brand":{"name":null}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_WriteNullLeaf_When_LeafValueIsNull()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              y: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id y } }",
            """{"data":{"foos":[{"id":"1","y":null}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, y: $__fusion_2_y) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement("__fusion_2_y", "y")
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotSource = new FetchResultStore();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);
        var entry = CreateEntry(
            snapshotSource,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")),
            Field("__fusion_2_y", NullValueNode.Default));

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_id", "__fusion_2_y"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"1","y":null}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_MergedPrefixHasNullAndPresentLeaves()
    {
        // arrange
        // The leaves under the merged 'y' prefix come from different requirements;
        // one resolves null while its sibling resolves a value.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              y: Y
            }

            type Y {
              z: Int
              w: Int
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { y { z w } } }",
            """{"data":{"foos":[{"y":{"z":null,"w":2}}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField("{ foo(a: $__fusion_1_a, b: $__fusion_2_b) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_a", "{ z: y.z }"),
            Requirement("__fusion_2_b", "{ w: y.w }")
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotSource = new FetchResultStore();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);
        var entry = CreateEntry(
            snapshotSource,
            CompactPath.Root,
            Field("__fusion_1_a", new ObjectValueNode(Field("z", NullValueNode.Default))),
            Field("__fusion_2_b", new ObjectValueNode(Field("w", new IntValueNode(2)))));

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_a", "__fusion_2_b"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","y":{"z":null,"w":2}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_KeyMapIsObjectValueSelection()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              things: [Thing]
            }

            type Thing {
              compositeId: CompositeId
            }

            type CompositeId {
              two: Int
              three: Int
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ things { compositeId { two three } } }",
            """{"data":{"things":[{"compositeId":{"two":2,"three":3}}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField("{ thingByCompositeId(key: $__fusion_1_key) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_key", "{ two: compositeId.two three: compositeId.three }")
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotSource = new FetchResultStore();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);
        var entry = CreateEntry(
            snapshotSource,
            CompactPath.Root,
            Field(
                "__fusion_1_key",
                new ObjectValueNode(
                    Field("two", new IntValueNode(2)),
                    Field("three", new IntValueNode(3)))));

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("things"),
            [],
            requirements,
            "Thing",
            lookupField);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            [entry],
            ImportedKeys("__fusion_1_key"),
            [],
            requirements,
            "Thing",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Thing","compositeId":{"two":2,"three":3}}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_RequireMapIsListSelection()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              products: [Product]
            }

            type Product {
              id: ID!
              similar: [Product]
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ products { id similar { id } } }",
            """{"data":{"products":[{"id":"1","similar":[{"id":"2"},{"id":"3"}]}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ productById(id: $__fusion_1_id, similar: $__fusion_2_similar) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement("__fusion_2_similar", "similar[{ id: id }]")
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("products"),
            [],
            requirements,
            "Product",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("products"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_similar"),
            [],
            requirements,
            "Product",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Product","id":"1","similar":[{"id":"2"},{"id":"3"}]}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SkipEntity_When_NonNullLeafValueIsNull()
    {
        // arrange
        // No null value can satisfy the non-null requirement type, so both the
        // live path and the extraction feeding the snapshot path skip the entity.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              y: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id y } }",
            """{"data":{"foos":[{"id":"1","y":null}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, y: $__fusion_2_y) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(schema, "__fusion_2_y", "y", new NonNullTypeNode(new NamedTypeNode("String")))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_y"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.True(live.IsEmpty);
        Assert.True(entries.IsDefaultOrEmpty);
        Assert.True(snapshot.IsEmpty);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_NonNullLeafSkipsEntity()
    {
        // arrange
        // The first entity's null value violates the non-null requirement type;
        // only the second entity is represented on both paths.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              y: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id y } }",
            """{"data":{"foos":[{"id":"1","y":null},{"id":"2","y":"v"}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, y: $__fusion_2_y) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(schema, "__fusion_2_y", "y", new NonNullTypeNode(new NamedTypeNode("String")))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_y"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"2","y":"v"}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SkipEntity_When_PathIntermediateIsNullAndLeafInputIsNonNull()
    {
        // arrange
        // A null intermediate resolves 'brand.name' to null, which cannot
        // satisfy the non-null requirement type, so the null structure must
        // not be emitted for the path chain.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              brand: Brand
            }

            type Brand {
              name: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id brand { name } } }",
            """{"data":{"foos":[{"id":"1","brand":null}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, bn: $__fusion_2_bn) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(
                schema,
                "__fusion_2_bn",
                "brand.name",
                new NonNullTypeNode(new NamedTypeNode("String")))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_bn"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.True(live.IsEmpty);
        Assert.True(entries.IsDefaultOrEmpty);
        Assert.True(snapshot.IsEmpty);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SkipEntity_When_NonNullListElementIsNull()
    {
        // arrange
        // The requirement type '[ProductKeyInput!]' does not allow null list
        // elements, so the entity with a null similar product is skipped.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              products: [Product]
              productsByKeys(keys: [ProductKeyInput!]): [Product]
            }

            type Product {
              id: ID!
              similar: [Product]
            }

            input ProductKeyInput {
              id: ID
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ products { id similar { id } } }",
            """{"data":{"products":[{"id":"1","similar":[{"id":"2"},null]}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ productById(id: $__fusion_1_id, similar: $__fusion_2_similar) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(
                schema,
                "__fusion_2_similar",
                "similar[{ id: id }]",
                new ListTypeNode(new NonNullTypeNode(new NamedTypeNode("ProductKeyInput"))))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("products"),
            [],
            requirements,
            "Product",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("products"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_similar"),
            [],
            requirements,
            "Product",
            lookupField);

        // assert
        Assert.True(live.IsEmpty);
        Assert.True(entries.IsDefaultOrEmpty);
        Assert.True(snapshot.IsEmpty);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_SkipEntity_When_NonNullLeafArrayElementIsNull()
    {
        // arrange
        // The requirement type '[String!]' does not allow null list elements,
        // so the entity with a null tag is skipped.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              tags: [String]
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id tags } }",
            """{"data":{"foos":[{"id":"1","tags":["a",null]}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, tags: $__fusion_2_tags) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(
                schema,
                "__fusion_2_tags",
                "tags",
                new ListTypeNode(new NonNullTypeNode(new NamedTypeNode("String"))))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_tags"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.True(live.IsEmpty);
        Assert.True(entries.IsDefaultOrEmpty);
        Assert.True(snapshot.IsEmpty);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_NullableListElementIsNull()
    {
        // arrange
        // The requirement type '[String]' allows null elements, so the null
        // tag ships as part of the list value on both paths.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              tags: [String]
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id tags } }",
            """{"data":{"foos":[{"id":"1","tags":["a",null]}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, tags: $__fusion_2_tags) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(
                schema,
                "__fusion_2_tags",
                "tags",
                new ListTypeNode(new NamedTypeNode("String")))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_tags"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"1","tags":["a",null]}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_NullableObjectListElementIsNull()
    {
        // arrange
        // The requirement type '[ProductKeyInput]' allows null elements, so
        // the null similar product ships as part of the list value on both paths.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              products: [Product]
              productsByKeys(keys: [ProductKeyInput]): [Product]
            }

            type Product {
              id: ID!
              similar: [Product]
            }

            input ProductKeyInput {
              id: ID
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ products { id similar { id } } }",
            """{"data":{"products":[{"id":"1","similar":[{"id":"2"},null]}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ productById(id: $__fusion_1_id, similar: $__fusion_2_similar) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(
                schema,
                "__fusion_2_similar",
                "similar[{ id: id }]",
                new ListTypeNode(new NamedTypeNode("ProductKeyInput")))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("products"),
            [],
            requirements,
            "Product",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("products"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_similar"),
            [],
            requirements,
            "Product",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Product","id":"1","similar":[{"id":"2"},null]}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_RequirementIsObjectValuedScalarList()
    {
        // arrange
        // Elements of a 'JSON' scalar list are JSON objects, which both paths
        // copy verbatim from the source document.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              dimensions: [JSON!]
            }

            scalar JSON
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id dimensions } }",
            """{"data":{"foos":[{"id":"1","dimensions":[{"width":1},{"height":2}]}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, dims: $__fusion_2_dims) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(
                schema,
                "__fusion_2_dims",
                "dimensions",
                new ListTypeNode(new NonNullTypeNode(new NamedTypeNode("JSON"))))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_dims"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"1","dimensions":[{"width":1},{"height":2}]}]}
            """);
    }

    [Fact]
    public void CreateRepresentationVariableValue_Should_ProduceSameBytes_As_SnapshotPath_When_RequirementIsObjectValuedScalar()
    {
        // arrange
        // The value of the 'JSON' scalar is a JSON object, which both paths
        // copy verbatim from the source document.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              size: JSON
            }

            scalar JSON
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id size } }",
            """{"data":{"foos":[{"id":"1","size":{"width":1,"height":2}}]}}""",
            resultArena,
            sourceArena);

        var lookupField = ParseLookupField(
            "{ fooById(id: $__fusion_1_id, size: $__fusion_2_size) { id } }");
        var requirements = new[]
        {
            Requirement("__fusion_1_id", "id"),
            Requirement(schema, "__fusion_2_size", "size", new NamedTypeNode("JSON"))
        };

        using var snapshotArena = new MemoryArena();
        using var snapshotTarget = CreateSnapshotStore(snapshotArena);

        // act
        var live = CreateRepresentation(
            store,
            schema,
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements,
            "Foo",
            lookupField);
        var entries = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            requirements);
        var snapshot = CreateRepresentationFromSnapshot(
            snapshotTarget,
            s_schema,
            entries,
            ImportedKeys("__fusion_1_id", "__fusion_2_size"),
            [],
            requirements,
            "Foo",
            lookupField);

        // assert
        Assert.Equal(snapshot.Value.AsSequence().ToArray(), live.Value.AsSequence().ToArray());
        Normalize(live.Value).MatchInlineSnapshot(
            """
            {"representations":[{"__typename":"Foo","id":"1","size":{"width":1,"height":2}}]}
            """);
    }

    private const string MergeSchemaA =
        """
        # name: a
        type Query {
          foos: [Foo]
          bars: [Bar]
        }

        type Foo @key(fields: "id") {
          id: ID!
        }

        type Bar @key(fields: "id") {
          id: ID!
        }
        """;

    private const string MergeSchemaB =
        """
        # name: b
        type Query {
          fooById(id: ID! @is(field: "id")): Foo @lookup @internal
          barById(id: ID! @is(field: "id")): Bar @lookup @internal
        }

        type Foo @key(fields: "id") {
          id: ID!
          name: String
        }

        type Bar @key(fields: "id") {
          id: ID!
          title: String
        }
        """;

    [Fact]
    public void AddRepresentationResult_Should_MergeEachLookup_When_TwoLookupResults()
    {
        // arrange
        var schema = ComposeSchema(MergeSchemaA, MergeSchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");
        var barDefinition = GetLookupDefinition(plan, "barById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"},{"id":"1"}],"bars":[{"id":"2"}]}}""",
            resultArena,
            sourceArena);

        var fooRepresentation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var barRepresentation = CreateLookupRepresentation(store, schema, "Bar", barDefinition);
        var fooResponse = CreateResponse(sourceArena, """{"data":{"_entities":[{"name":"a"}]}}""");
        var barResponse = CreateResponse(sourceArena, """{"data":{"_entities":[{"title":"t"}]}}""");

        // act
        // The duplicate foo representation fans the single entity out to both paths.
        var fooAdded = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            fooResponse,
            fooRepresentation,
            fooDefinition.ResultSelectionSet,
            containsErrors: false);
        var barAdded = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            barResponse,
            barRepresentation,
            barDefinition.ResultSelectionSet,
            containsErrors: false);

        // assert
        Assert.True(fooAdded);
        Assert.True(barAdded);
        RenderData(store).MatchInlineSnapshot(
            """
            {"foos":[{"id":"1","name":"a"},{"id":"1","name":"a"}],"bars":[{"id":"2","title":"t"}]}
            """);
    }

    [Fact]
    public void AddRepresentationResult_Should_RouteErrorTrie_When_SubgraphErrorPathUsesEntities()
    {
        // arrange
        var schema = ComposeSchema(MergeSchemaA, MergeSchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");
        var barDefinition = GetLookupDefinition(plan, "barById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[{"id":"2"}]}}""",
            resultArena,
            sourceArena);

        var fooRepresentation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var barRepresentation = CreateLookupRepresentation(store, schema, "Bar", barDefinition);
        var fooResponse = CreateResponse(
            sourceArena,
            """
            {"data":{"_entities":[{"name":null}]},
             "errors":[{"message":"boom","path":["_entities",0,"name"]}]}
            """);
        var barResponse = CreateResponse(sourceArena, """{"data":{"_entities":[{"title":"t"}]}}""");

        // act
        var fooAdded = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            fooResponse,
            fooRepresentation,
            fooDefinition.ResultSelectionSet,
            containsErrors: true);
        var barAdded = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            barResponse,
            barRepresentation,
            barDefinition.ResultSelectionSet,
            containsErrors: false);

        // assert
        // The '_entities' subgraph path is rewritten to the client-facing path.
        store.FinalizePocketedErrors();
        Assert.True(fooAdded && barAdded);
        var error = Assert.Single(store.Errors!);
        Assert.Equal("boom", error.Message);
        Assert.Equal("foos[0].name", error.Path!.Print());
        RenderData(store).MatchInlineSnapshot(
            """
            {"foos":[{"id":"1","name":null}],"bars":[{"id":"2","title":"t"}]}
            """);
    }

    [Fact]
    public void AddRepresentationResult_Should_PreserveRootErrorWithoutMerging_When_EntitiesDataIsMissing()
    {
        // arrange
        var schema = ComposeSchema(MergeSchemaA, MergeSchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[]}}""",
            resultArena,
            sourceArena);

        var representation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var response = CreateResponse(
            sourceArena,
            """{"errors":[{"message":"boom"}]}""");
        var before = RenderData(store);
        var added = false;

        // act
        var exception = Record.Exception(
            () => added = store.AddRepresentationResult(
                SelectionPath.Root.AppendField("_entities"),
                response,
                representation,
                fooDefinition.ResultSelectionSet,
                containsErrors: true));

        // assert
        store.FinalizePocketedErrors();
        var error = Assert.Single(store.Errors!);
        Assert.Equal("boom", error.Message);
        Assert.Null(error.Path);
        Assert.Equal(before, RenderData(store));
        Assert.Null(exception);
        Assert.True(added);
    }

    [Fact]
    public void AddRepresentationResult_Should_RebaseEntityErrorWithoutMerging_When_EntitiesDataIsMissing()
    {
        // arrange
        var schema = ComposeSchema(MergeSchemaA, MergeSchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[]}}""",
            resultArena,
            sourceArena);

        var representation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var response = CreateResponse(
            sourceArena,
            """{"errors":[{"message":"boom","path":["_entities",0,"name"]}]}""");
        var before = RenderData(store);

        // act
        var added = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            response,
            representation,
            fooDefinition.ResultSelectionSet,
            containsErrors: true);

        // assert
        store.FinalizePocketedErrors();
        Assert.True(added);
        var error = Assert.Single(store.Errors!);
        Assert.Equal("boom", error.Message);
        Assert.Equal("foos[0].name", error.Path!.Print());
        Assert.Equal(before, RenderData(store));
    }

    [Fact]
    public void AddRepresentationResult_Should_PropagateNonNullField_When_EntitiesDataIsMissing()
    {
        // arrange
        var schema = ComposeSchema(
            MergeSchemaA,
            MergeSchemaB.Replace("name: String", "name: String!", StringComparison.Ordinal));
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[]}}""",
            resultArena,
            sourceArena);

        var representation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var response = CreateResponse(
            sourceArena,
            """{"errors":[{"message":"boom","path":["_entities",0,"name"]}]}""");

        // act
        var added = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            response,
            representation,
            fooDefinition.ResultSelectionSet,
            containsErrors: true);

        // assert
        store.FinalizePocketedErrors();
        Assert.True(added);
        var error = Assert.Single(store.Errors!);
        Assert.Equal("boom", error.Message);
        Assert.Equal("foos[0].name", error.Path!.Print());
        RenderData(store).MatchInlineSnapshot(
            """
            {"foos":[null],"bars":[]}
            """);
    }

    [Fact]
    public void AddRepresentationResult_Should_NullNullableAncestor_When_NestedErrorLeafIsUnreachable()
    {
        // arrange
        var schemaB = MergeSchemaB.Replace(
            "name: String",
            "details: Details\n        }\n\n        type Details {\n          name: String!",
            StringComparison.Ordinal);
        var schema = ComposeSchema(MergeSchemaA, schemaB);
        var plan = PlanOperation(schema, "{ foos { id details { name } } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[]}}""",
            resultArena,
            sourceArena);

        var representation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var response = CreateResponse(
            sourceArena,
            """{"errors":[{"message":"boom","path":["_entities",0,"details","name"]}]}""");

        // act
        var added = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            response,
            representation,
            fooDefinition.ResultSelectionSet,
            containsErrors: true);

        // assert
        store.FinalizePocketedErrors();
        Assert.True(added);
        var error = Assert.Single(store.Errors!);
        Assert.Equal("boom", error.Message);
        Assert.Equal("foos[0].details.name", error.Path!.Print());
        RenderData(store).MatchInlineSnapshot(
            """
            {"foos":[{"id":"1","details":null}],"bars":[]}
            """);
    }

    [Fact]
    public void AddRepresentationResult_Should_PropagateNonNullAncestor_When_NestedErrorLeafIsUnreachable()
    {
        // arrange
        var schemaB = MergeSchemaB.Replace(
            "name: String",
            "details: Details!\n        }\n\n        type Details {\n          name: String!",
            StringComparison.Ordinal);
        var schema = ComposeSchema(MergeSchemaA, schemaB);
        var plan = PlanOperation(schema, "{ foos { id details { name } } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[]}}""",
            resultArena,
            sourceArena);

        var representation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var response = CreateResponse(
            sourceArena,
            """{"errors":[{"message":"boom","path":["_entities",0,"details","name"]}]}""");

        // act
        var added = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            response,
            representation,
            fooDefinition.ResultSelectionSet,
            containsErrors: true);

        // assert
        store.FinalizePocketedErrors();
        Assert.True(added);
        var error = Assert.Single(store.Errors!);
        Assert.Equal("boom", error.Message);
        Assert.Equal("foos[0].details.name", error.Path!.Print());
        RenderData(store).MatchInlineSnapshot(
            """
            {"foos":[null],"bars":[]}
            """);
    }

    [Fact]
    public void AddRepresentationResult_Should_ApplyErrorHandler_When_EntitiesDataIsMissing()
    {
        // arrange
        var schema = ComposeSchema(MergeSchemaA, MergeSchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[]}}""",
            resultArena,
            sourceArena,
            new MaskingErrorHandler());

        var representation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var response = CreateResponse(
            sourceArena,
            """{"errors":[{"message":"boom","path":["_entities",0,"name"]}]}""");

        // act
        var added = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            response,
            representation,
            fooDefinition.ResultSelectionSet,
            containsErrors: true);

        // assert
        store.FinalizePocketedErrors();
        Assert.True(added);
        var error = Assert.Single(store.Errors!);
        Assert.Equal("masked", error.Message);
        Assert.Equal("foos[0].name", error.Path!.Print());
    }

    [Fact]
    public void AddRepresentationResult_Should_AddTargetedError_When_EntityCountDoesNotMatchRepresentations()
    {
        // arrange
        var schema = ComposeSchema(MergeSchemaA, MergeSchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[]}}""",
            resultArena,
            sourceArena);

        var representation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var response = CreateResponse(sourceArena, """{"data":{"_entities":[]}}""");

        // act
        var added = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            response,
            representation,
            fooDefinition.ResultSelectionSet,
            containsErrors: false);

        // assert
        store.FinalizePocketedErrors();
        Assert.True(added);
        var error = Assert.Single(store.Errors!);
        Assert.Equal(
            "The representation result at '$._entities' contains 0 entries, but 1 were expected.",
            error.Message);
        Assert.Equal("foos[0].name", error.Path!.Print());
    }

    [Theory]
    [InlineData("{}", "Undefined")]
    [InlineData("{\"data\":{\"_entities\":null}}", "Null")]
    [InlineData("{\"data\":{\"_entities\":{}}}", "Object")]
    public void AddRepresentationResult_Should_AddTargetedError_When_EntitiesDataIsNotArray(
        string responseJson,
        string valueKind)
    {
        // arrange
        var schema = ComposeSchema(MergeSchemaA, MergeSchemaB);
        var plan = PlanOperation(schema, "{ foos { id name } bars { id title } }");
        var fooDefinition = GetLookupDefinition(plan, "fooById");

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateMergeStore(
            schema,
            plan,
            """{"data":{"foos":[{"id":"1"}],"bars":[]}}""",
            resultArena,
            sourceArena);

        var representation = CreateLookupRepresentation(store, schema, "Foo", fooDefinition);
        var response = CreateResponse(sourceArena, responseJson);

        // act
        var added = store.AddRepresentationResult(
            SelectionPath.Root.AppendField("_entities"),
            response,
            representation,
            fooDefinition.ResultSelectionSet,
            containsErrors: false);

        // assert
        store.FinalizePocketedErrors();
        Assert.True(added);
        var error = Assert.Single(store.Errors!);
        Assert.Equal(
            $"The representation result at '$._entities' must be an array but was '{valueKind}'.",
            error.Message);
        Assert.Equal("foos[0].name", error.Path!.Print());
    }

    private static FetchResultStore CreateMergeStore(
        FusionSchemaDefinition schema,
        OperationPlan plan,
        string payloadJson,
        MemoryArena resultArena,
        MemoryArena sourceArena)
        => CreateMergeStore(
            schema,
            plan,
            payloadJson,
            resultArena,
            sourceArena,
            DefaultErrorHandler.Default);

    private static FetchResultStore CreateMergeStore(
        FusionSchemaDefinition schema,
        OperationPlan plan,
        string payloadJson,
        MemoryArena resultArena,
        MemoryArena sourceArena,
        IErrorHandler errorHandler)
    {
        var root = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            errorHandler,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var payload = Encoding.UTF8.GetBytes(payloadJson);
        var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            root.ResultSelectionSet,
            containsErrors: false);
        Assert.True(added);

        return store;
    }

    private sealed class MaskingErrorHandler : IErrorHandler
    {
        public IError Handle(IError error)
            => ErrorBuilder.FromError(error)
                .SetMessage("masked")
                .Build();
    }

    private static SingleOperationDefinition GetLookupDefinition(
        OperationPlan plan,
        string lookupFieldName)
    {
        foreach (var node in plan.AllNodes)
        {
            if (node is not OperationBatchExecutionNode batchNode)
            {
                continue;
            }

            foreach (var operation in batchNode.Operations)
            {
                if (operation is SingleOperationDefinition definition
                    && definition.Operation.SourceText.Contains(lookupFieldName, StringComparison.Ordinal))
                {
                    return definition;
                }
            }
        }

        throw new InvalidOperationException(
            $"The plan does not contain a lookup definition for '{lookupFieldName}'.");
    }

    private static RepresentationValue CreateLookupRepresentation(
        FetchResultStore store,
        FusionSchemaDefinition schema,
        string entityTypeName,
        SingleOperationDefinition definition)
        => CreateRepresentation(
            store,
            schema,
            definition.Target,
            [],
            definition.Requirements,
            entityTypeName,
            ParseLookupField(definition.Operation.SourceText));

    // The representation shape is a plan-time constant, so the production code
    // builds it once at node creation. These harness helpers mirror that by
    // compiling the shape and passing it in.
    private static RepresentationValue CreateRepresentation(
        FetchResultStore store,
        FusionSchemaDefinition schema,
        SelectionPath selectionSet,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requirements,
        string entityTypeName,
        FieldNode lookupField)
        => store.CreateRepresentationVariableValue(
            selectionSet,
            requestVariables,
            requirements,
            entityTypeName,
            RepresentationShapeBuilder.Build(lookupField, requirements, schema, entityTypeName));

    private static RepresentationValue CreateRepresentationFromSnapshot(
        FetchResultStore store,
        FusionSchemaDefinition schema,
        ImmutableArray<VariableValues> importedEntries,
        HashSet<string> importedKeys,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requirements,
        string entityTypeName,
        FieldNode lookupField)
        => store.CreateRepresentationVariableValueFromSnapshot(
            importedEntries,
            importedKeys,
            requestVariables,
            requirements,
            entityTypeName,
            RepresentationShapeBuilder.Build(lookupField, requirements, schema, entityTypeName));

    private static SourceSchemaResult CreateResponse(MemoryArena arena, string payloadJson)
    {
        var payload = Encoding.UTF8.GetBytes(payloadJson);
        var document = SourceResultDocument.Parse(arena, payload, payload.Length);
        return new SourceSchemaResult(CompactPath.Root, document);
    }

    private static string RenderData(FetchResultStore store)
    {
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, new JsonWriterOptions { Indented = false });
        store.Result.WriteDataTo(writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static FetchResultStore CreateLiveStore(
        FusionSchemaDefinition schema,
        string operationText,
        string payloadJson,
        MemoryArena resultArena,
        MemoryArena sourceArena)
    {
        var plan = PlanOperation(schema, operationText);
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var payload = Encoding.UTF8.GetBytes(payloadJson);
        var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            node.ResultSelectionSet,
            containsErrors: false);
        Assert.True(added);

        return store;
    }

    private static FetchResultStore CreateSnapshotStore(MemoryArena arena)
        => CreateSnapshotStore(arena, s_schema, s_operation);

    private static FetchResultStore CreateSnapshotStore(
        MemoryArena arena,
        FusionSchemaDefinition schema,
        Operation operation)
    {
        var store = new FetchResultStore();
        store.Initialize(
            arena,
            schema,
            DefaultErrorHandler.Default,
            operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);
        return store;
    }

    private static FieldNode ParseLookupField(string operationText)
    {
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().First();
        return (FieldNode)operation.SelectionSet.Selections[0];
    }

    private static VariableValues CreateEntry(
        FetchResultStore store,
        CompactPath path,
        params ObjectFieldNode[] fields)
        => store.CreateVariableValueSets(path, fields);

    private static ObjectFieldNode Field(string name, IValueNode value)
        => new(name, value);

    private static OperationRequirement Requirement(string key, string map)
        => Requirement(s_schema, key, map, new NamedTypeNode("String"));

    private static OperationRequirement Requirement(
        FusionSchemaDefinition schema,
        string key,
        string map,
        ITypeNode type)
        => new(
            key,
            type,
            SelectionPath.Root,
            new FieldSelectionMapParser(map).Parse(),
            null);

    private static HashSet<string> ImportedKeys(params string[] keys)
        => new(keys, StringComparer.Ordinal);

    private static CompactPath Path(params int[] segments)
    {
        var buffer = new int[segments.Length + 1];
        buffer[0] = segments.Length;
        segments.CopyTo(buffer.AsSpan(1));
        return new CompactPath(buffer);
    }

    private static string Normalize(JsonSegment segment)
    {
        using var document = JsonDocument.Parse(segment.AsSequence());
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static SelectionSetNode RenderShape(List<RepresentationShapeNode> level)
        => new(level.Select(RenderShapeNode).ToList<ISelectionNode>());

    private static FieldNode RenderShapeNode(RepresentationShapeNode node)
        => new(
            location: null,
            new NameNode(node.Name),
            alias: null,
            directives: [],
            arguments: [],
            node.Children is null ? null : RenderShape(node.Children));
}
