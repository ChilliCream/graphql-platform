using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Results;

public sealed class ValueCompletionMergeProgramTests : FusionTestBase
{
    private const string ProductSchema =
        """
        # name: test
        type Query {
          product: Product
          products: [Product!]!
          entry: Entry
          shapes: [Shape]
        }

        type Product {
          id: ID!
          name: String!
          description: String
          price: Int!
          tags: [String!]
          ratings: [Int]
          matrix: [[Int]]
          status: Status
          sizes: [Size!]
          dimension: Dimension
          variants: [Variant!]!
        }

        type Dimension {
          height: Int!
          width: Int
        }

        type Variant {
          sku: String!
          size: Size
        }

        enum Status {
          ACTIVE
          RETIRED
        }

        enum Size {
          S
          M
          L
        }

        interface Entry {
          id: ID!
        }

        type Book implements Entry {
          id: ID!
          title: String!
        }

        type Author implements Entry {
          id: ID!
          name: String!
        }

        union Shape = Circle | Square

        type Circle {
          radius: Int!
        }

        type Square {
          side: Int!
        }
        """;

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_SourceReordersFields()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { id name price dimension { height width } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"product":{"price":899,"dimension":{"width":20,"height":10},"name":"Table","id":"1"}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"product":{"id":"1","name":"Table","price":899,"dimension":{"height":10,"width":20}}}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_SourceOmitsAndAddsFields()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { id name description price dimension { height width } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"product":{"__typename":"Product","id":"1","unknown":{"a":[1,2]},"price":899,"dimension":{"height":10}}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"product":{"id":"1","name":null,"description":null,"price":899,"dimension":{"height":10,"width":null}}}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_NullableValuesAreNull()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(
            schema,
            "{ product { id description tags ratings status dimension { height } variants { sku size } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"product":{"id":"1","description":null,"tags":null,"ratings":[1,null,3],"status":null,"dimension":null,"variants":[{"sku":"a","size":null}]}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"product":{"id":"1","description":null,"tags":null,"ratings":[1,null,3],"status":null,"dimension":null,"variants":[{"sku":"a","size":null}]}}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_NestedNonNullFieldIsNull()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { id dimension { height width } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"product":{"id":"1","dimension":{"height":null,"width":20}}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"product":null}
            errors:
            - Cannot return null for non-nullable field. @ product.dimension.height
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_NonNullListElementIsNull()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ products { id variants { sku } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"products":[{"id":"1","variants":[{"sku":"a"},null]},{"id":"2","variants":[]}]}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: false
            {"products":[{"id":"1","variants":[{"sku":"a"},null]},null]}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_ResultContainsErrors()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { id description dimension { width } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"errors":[{"message":"boom","path":["product","description"]}],"data":{"product":{"id":"1","description":null,"dimension":{"width":1}}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload, containsErrors: true);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"product":{"id":"1","description":null,"dimension":{"width":1}}}
            errors:
            - boom @ product.description
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_ListsHoldScalarsAndNestedLists()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { tags ratings matrix sizes } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"product":{"tags":["a","b"],"ratings":[1,null,3],"matrix":[[1,2],[],null,[3]],"sizes":["S","XL","L"]}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"product":{"tags":["a","b"],"ratings":[1,null,3],"matrix":[[1,2],[],null,[3]],"sizes":["S",null,"L"]}}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_EnumValuesAreMasked()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { status variants { sku size } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"product":{"status":"ACTIVE","variants":[{"sku":"a","size":"XL"},{"sku":"b","size":"M"},{"sku":"c","size":1}]}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"product":{"status":"ACTIVE","variants":[{"sku":"a","size":null},{"sku":"b","size":"M"},{"sku":"c","size":1}]}}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_AbstractTypesAreSelected()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(
            schema,
            "{ entry { id ... on Book { title } ... on Author { name } } shapes { ... on Circle { radius } ... on Square { side } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"entry":{"__typename":"Author","id":"1","name":"Ann"},"shapes":[{"__typename":"Square","side":2},null,{"__typename":"Circle","radius":3}]}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"entry":{"id":"1","name":"Ann"},"shapes":[{"side":2},null,{"radius":3}]}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_FieldsAreAliased()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ first: product { n: name id } second: product { id } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"second":{"id":"2"},"first":{"id":"1","n":"Table"}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"first":{"n":"Table","id":"1"},"second":{"id":"2"}}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_FragmentsSplitAnObjectSelection()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(
            schema,
            "{ product { ... on Product { dimension { height } } dimension { width } variants { ... on Variant { sku } } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string payload =
            """{"data":{"product":{"dimension":{"width":2,"height":1},"variants":[{"size":"M","sku":"a"}]}}}""";

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"product":{"dimension":{"height":1,"width":2},"variants":[{"sku":"a"}]}}
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_ObjectListsSpanMetaDbChunks()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ products { id name variants { sku size } } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var payload = new StringBuilder("""{"data":{"products":[""");

        for (var i = 0; i < 120; i++)
        {
            if (i > 0)
            {
                payload.Append(',');
            }

            payload.Append("{\"id\":\"p").Append(i)
                .Append("\",\"name\":\"Product ").Append(i)
                .Append("\",\"variants\":[{\"sku\":\"a").Append(i)
                .Append("\",\"size\":\"S\"},{\"size\":\"L\",\"sku\":\"b").Append(i)
                .Append("\"}]}");
        }

        payload.Append("]}}");

        // act
        var (program, generic) = MergeBoth(schema, plan, node, payload.ToString());

        // assert
        Assert.Equal(generic, program);
        Assert.StartsWith(
            """
            completed: true
            {"products":[{"id":"p0","name":"Product 0","variants":[{"sku":"a0","size":"S"},{"sku":"b0","size":"L"}]},
            """,
            program);
        Assert.EndsWith(
            """{"id":"p119","name":"Product 119","variants":[{"sku":"a119","size":"S"},{"sku":"b119","size":"L"}]}]}""",
            program);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_SharedListLengthsDiffer()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ products { id } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        const string first = """{"data":{"products":[{"id":"1"},{"id":"2"}]}}""";
        const string second = """{"data":{"products":[{"id":"1"}]}}""";

        // act
        var program = MergeSequence(schema, plan, node, [first, second], useProgram: true);
        var generic = MergeSequence(schema, plan, node, [first, second], useProgram: false);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: false
            {"products":[{"id":"1"},{"id":"2"}]}
            errors:
            - Cannot merge shared list results with different lengths. @ products
            """);
    }

    [Fact]
    public void BuildResult_Should_MatchGenericCompletion_When_LookupResultsMergeIntoExistingObjects()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: accounts
            type Query {
              users: [User!]!
              userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
              id: ID!
              name: String!
            }
            """,
            """
            # name: reviews
            type Query {
              userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              reviews: [Review!]!
            }

            type Review {
              id: ID!
              body: String
            }
            """);
        var plan = PlanOperation(schema, "{ users { id name reviews { id body } } }");
        var rootNode = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var lookupNode = plan.AllNodes.OfType<OperationExecutionNode>().Single(t => t.Target.ToString() == "$.users");
        const string users = """{"data":{"users":[{"id":"1","name":"Ann"},{"id":"2","name":"Ben"}]}}""";
        string[] lookups =
        [
            """{"data":{"userById":{"reviews":[{"body":"nice","id":"r1"},{"id":"r2","body":null}]}}}""",
            """{"data":{"userById":{"reviews":[]}}}"""
        ];

        // act
        var program = MergeLookup(schema, plan, rootNode, lookupNode, users, lookups, useProgram: true);
        var generic = MergeLookup(schema, plan, rootNode, lookupNode, users, lookups, useProgram: false);

        // assert
        Assert.Equal(generic, program);
        program.MatchInlineSnapshot(
            """
            completed: true
            {"users":[{"id":"1","name":"Ann","reviews":[{"id":"r1","body":"nice"},{"id":"r2","body":null}]},{"id":"2","name":"Ben","reviews":[]}]}
            """);
    }

    [Fact]
    public void TryGetMergeProgram_Should_FollowSourceOrder_When_SelectionSetIsResolved()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(
            schema,
            "{ product { dimension { width height } name tags status sizes matrix variants { size sku } entry: id } entry { id } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));

        // act
        var found = node.ResultSelectionSet.TryGetMergeProgram(plan.Operation.RootSelectionSet, out var program);

        // assert
        Assert.True(found);
        Render(program!).MatchInlineSnapshot(
            """
            product: Object {
              dimension: Object {
                width: Generic
                height: Generic
              }
              name: Generic
              tags: LeafList
              status: Generic
              sizes: EnumList
              matrix: Generic
              variants: ObjectList {
                size: Generic
                sku: Generic
              }
              entry: Generic
            }
            entry: Generic
            """);
    }

    [Fact]
    public void TryGetMergeProgram_Should_ReturnFalse_When_SourceResponseNamesAreMapped()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { price } }");
        var selectionSetNode = Utf8GraphQLParser.Syntax.ParseSelectionSet(
            """{ fusion__requirement_0: product @fusion__responseName(name: "product") { price } }""");
        var resultSelectionSet = ResultSelectionSet.CreateFromPlan(selectionSetNode, schema);
        resultSelectionSet.SetMergeTemplate(MergeProgramTemplate.Create(selectionSetNode));

        // act
        var found = resultSelectionSet.TryGetMergeProgram(plan.Operation.RootSelectionSet, out var program);

        // assert
        Assert.False(found);
        Assert.Null(program);
    }

    [Fact]
    public void JsonOperationPlanParser_Should_RestoreMergeTemplate_When_PlanIsParsed()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { id dimension { width height } variants { sku } } }");
        var planned = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var json = new JsonOperationPlanFormatter().Format(plan);
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var parser = new JsonOperationPlanParser(new OperationCompiler(schema, pool));

        // act
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(json));
        var parsed = Assert.IsType<OperationExecutionNode>(Assert.Single(parsedPlan.RootNodes));

        // assert
        Assert.Equal(Render(planned.ResultSelectionSet.MergeTemplate!), Render(parsed.ResultSelectionSet.MergeTemplate!));
        Render(parsed.ResultSelectionSet.MergeTemplate!).MatchInlineSnapshot(
            """
            product {
              id
              dimension {
                width
                height
              }
              variants {
                sku
              }
            }
            """);
    }

    private static (string Program, string Generic) MergeBoth(
        FusionSchemaDefinition schema,
        OperationPlan plan,
        OperationExecutionNode node,
        string payload,
        bool containsErrors = false)
        => (
            MergeSequence(schema, plan, node, [payload], useProgram: true, containsErrors),
            MergeSequence(schema, plan, node, [payload], useProgram: false, containsErrors));

    private static string MergeSequence(
        FusionSchemaDefinition schema,
        OperationPlan plan,
        OperationExecutionNode node,
        string[] payloads,
        bool useProgram,
        bool containsErrors = false)
    {
        using var scope = new ProgramScope(node.ResultSelectionSet, useProgram);
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateStore(schema, plan, resultArena);
        var completed = true;

        foreach (var payload in payloads)
        {
            completed &= store.AddPartialResult(
                node.Source,
                new SourceSchemaResult(CompactPath.Root, Parse(sourceArena, payload)),
                node.ResultSelectionSet,
                containsErrors);
        }

        return Render(store, completed);
    }

    private static string MergeLookup(
        FusionSchemaDefinition schema,
        OperationPlan plan,
        OperationExecutionNode rootNode,
        OperationExecutionNode lookupNode,
        string rootPayload,
        string[] lookupPayloads,
        bool useProgram)
    {
        using var rootScope = new ProgramScope(rootNode.ResultSelectionSet, useProgram);
        using var lookupScope = new ProgramScope(lookupNode.ResultSelectionSet, useProgram);
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateStore(schema, plan, resultArena);
        var completed = store.AddPartialResult(
            rootNode.Source,
            new SourceSchemaResult(CompactPath.Root, Parse(sourceArena, rootPayload)),
            rootNode.ResultSelectionSet,
            containsErrors: false);

        var paths = store.GetResultPaths(lookupNode.Target);
        var results = new SourceSchemaResult[lookupPayloads.Length];

        for (var i = 0; i < results.Length; i++)
        {
            results[i] = new SourceSchemaResult(paths[i], Parse(sourceArena, lookupPayloads[i]));
        }

        completed &= store.AddPartialResults(
            lookupNode.Source,
            results,
            lookupNode.ResultSelectionSet,
            containsErrors: false);

        return Render(store, completed);
    }

    private static FetchResultStore CreateStore(
        FusionSchemaDefinition schema,
        OperationPlan plan,
        MemoryArena resultArena)
    {
        var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: default,
            deferFlags: default,
            pathSegmentLocalPoolCapacity: 16);
        return store;
    }

    private static SourceResultDocument Parse(MemoryArena arena, string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        return SourceResultDocument.Parse(arena, bytes, bytes.Length);
    }

    private static string Render(FetchResultStore store, bool completed)
    {
        using var buffer = new PooledArrayWriter();
        store.Result.Data.WriteTo(buffer);

        var text = new StringBuilder();
        text.Append("completed: ").Append(completed ? "true" : "false").Append('\n');
        text.Append(Encoding.UTF8.GetString(buffer.WrittenSpan));

        if (store.Errors is { Count: > 0 } errors)
        {
            text.Append("\nerrors:");

            foreach (var error in errors)
            {
                text.Append("\n- ").Append(error.Message).Append(" @ ").Append(error.Path?.Print());
            }
        }

        return text.ToString();
    }

    private static string Render(MergeProgram program)
    {
        var text = new StringBuilder();
        Render(program, text, indent: 0);
        return text.ToString().TrimEnd();
    }

    private static void Render(MergeProgram program, StringBuilder text, int indent)
    {
        for (var i = 0; i < program.Length; i++)
        {
            ref readonly var entry = ref program[i];
            text.Append(' ', indent)
                .Append(Encoding.UTF8.GetString(program.GetName(in entry)))
                .Append(": ")
                .Append(entry.Op);

            if (entry.Child is { } child)
            {
                text.Append(" {\n");
                Render(child, text, indent + 2);
                text.Append(' ', indent).Append("}\n");
            }
            else
            {
                text.Append('\n');
            }
        }
    }

    private static string Render(MergeProgramTemplate template)
    {
        var text = new StringBuilder();
        Render(template, text, indent: 0);
        return text.ToString().TrimEnd();
    }

    private static void Render(MergeProgramTemplate template, StringBuilder text, int indent)
    {
        foreach (var entry in template.Entries)
        {
            text.Append(' ', indent).Append(entry.ResponseName);

            if (entry.Child is { } child)
            {
                text.Append(" {\n");
                Render(child, text, indent + 2);
                text.Append(' ', indent).Append("}\n");
            }
            else
            {
                text.Append('\n');
            }
        }
    }

    /// <summary>
    /// Runs a merge with or without the merge program of a result selection set and restores
    /// the template afterwards.
    /// </summary>
    private sealed class ProgramScope : IDisposable
    {
        private readonly ResultSelectionSet _resultSelectionSet;
        private readonly MergeProgramTemplate _template;

        public ProgramScope(ResultSelectionSet resultSelectionSet, bool useProgram)
        {
            _resultSelectionSet = resultSelectionSet;
            _template = resultSelectionSet.MergeTemplate
                ?? throw new InvalidOperationException("The planner did not attach a merge template.");

            if (!useProgram)
            {
                resultSelectionSet.SetMergeTemplate(null);
            }
        }

        public void Dispose() => _resultSelectionSet.SetMergeTemplate(_template);
    }
}
