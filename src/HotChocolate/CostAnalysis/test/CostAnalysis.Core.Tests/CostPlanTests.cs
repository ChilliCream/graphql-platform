using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public sealed class CostPlanTests
{
    private const string Directives =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], slicingArgumentDefaultValue: Float, sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
        """ + "\n";

    [Fact]
    public void Evaluate_Should_PriceComplementaryConditions_When_VariableIsFalse()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Side { costly: Int @cost(weight: "10") }
              type Query { left: Side right: Side }
              """,
            "query Example($x: Boolean!) { left { costly @include(if: $x) } right { costly @skip(if: $x) } }");

        // act
        var estimate = plan.Evaluate(Variables(("x", BooleanValueNode.False)));

        // assert
        Assert.Equal(new CostEstimate(12.0, 3.0, null), estimate);
    }

    [Fact]
    public void EvaluateStaticBound_Should_PriceComplementaryConditions_When_VariableIsUnknown()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Side { costly: Int @cost(weight: "10") }
              type Query { left: Side right: Side }
              """,
            "query Example($x: Boolean!) { left { costly @include(if: $x) } right { costly @skip(if: $x) } }");

        // act
        var estimate = plan.EvaluateStaticBound();

        // assert
        Assert.Equal(new CostEstimate(12.0, 3.0, null), estimate);
    }

    [Theory]
    [InlineData("mutation { createBook(title: \"x\") { title } }", 4.0, 1.0)]
    [InlineData("mutation { a: createBook(title: \"x\") { title } b: createBook(title: \"y\") { title } }", 5.0, 2.0)]
    [InlineData("subscription { onBookAdded { title } }", 2.0, 1.0)]
    public void Evaluate_Should_PriceOperationRootOnce_When_OperationTypeVaries(
        string operation,
        double typeCost,
        double fieldCost)
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Query { book: Book }
              type Mutation @cost(weight: "3") { createBook(title: String): Book }
              type Subscription { onBookAdded: Book }
              type Book { title: String }
              """,
            operation);

        // act
        var estimate = plan.Evaluate(Variables());

        // assert
        Assert.Equal(new CostEstimate(fieldCost, typeCost, null), estimate);
    }

    [Theory]
    [InlineData("mutation", "Mutation")]
    [InlineData("subscription", "Subscription")]
    public void Evaluate_Should_PriceOperation_When_SchemaOmitsQueryRoot(
        string operationType,
        string rootTypeName)
    {
        // arrange
        var plan = Compile(
            Directives
            + $$"""
              schema { {{operationType}}: {{rootTypeName}} }
              type {{rootTypeName}} @cost(weight: "3") {
                value: String @cost(weight: "2")
              }
              """,
            $"{operationType} {{ value }}");

        // act
        var estimate = plan.Evaluate(Variables());

        // assert
        Assert.Equal(new CostEstimate(2.0, 3.0, null), estimate);
    }

    [Fact]
    public void Compile_Should_Throw_When_OperationRootIsNotDefined()
    {
        // arrange
        const string operation = "query { value }";

        // act
        var error = Assert.Throws<InvalidOperationException>(() =>
            Compile(
                Directives
                + """
                  schema { mutation: Mutation }
                  type Mutation { value: String }
                  """,
                operation));

        // assert
        Assert.Equal("The schema does not define a root type for 'Query'.", error.Message);
    }

    [Fact]
    public void Evaluate_Should_ResolveInheritedSizedFieldVariable_When_PlanIsCached()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Item { value: Int @cost(weight: "2") }
              type Container { items: [Item] }
              type Query {
                container(limit: Int!): Container
                  @listSize(assumedSize: 10, slicingArguments: ["limit"], sizedFields: ["items"])
              }
              """,
            "query($n: Int!) { container(limit: $n) { items { value } } }");

        // act
        var actual = plan.Evaluate(Variables(("n", new IntValueNode(3))));
        var bound = plan.EvaluateStaticBound();

        // assert
        Assert.Equal(new CostEstimate(8.0, 5.0, null), actual);
        Assert.Equal(new CostEstimate(22.0, 12.0, null), bound);
    }

    [Fact]
    public void Evaluate_Should_IncludeResponseSize_When_Requested()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Item { value: Int }
              type Query { items(limit: Int!): [[Item]] @listSize(slicingArguments: ["limit"]) }
              """,
            "query($n: Int!) { items(limit: $n) { value } }",
            CostAnalyses.Cost | CostAnalyses.ResponseSize);

        // act
        var estimate = plan.Evaluate(Variables(("n", new IntValueNode(3))));

        // assert
        Assert.Equal(new CostEstimate(1.0, 4.0, 10.0), estimate);
    }

    [Fact]
    public void Evaluate_Should_MatchDynamicPlan_When_StaticInputsHaveFractionalAndLargeWeights()
    {
        // arrange
        const string schema = Directives
            + """
              type Item @cost(weight: "0.25") {
                value(scale: Float @cost(weight: "0.125")): Float
                  @cost(weight: "9007199254740992")
              }
              type Query @cost(weight: "0.5") {
                items(limit: Int): [Item]
                  @cost(weight: "0.375")
                  @listSize(slicingArguments: ["limit"])
              }
              """;
        var staticPlan = Compile(
            schema,
            "{ items(limit: 3) { value(scale: 0.5) } }",
            CostAnalyses.Cost | CostAnalyses.ResponseSize);
        var dynamicPlan = Compile(
            schema,
            "query($limit: Int) { items(limit: $limit) { value(scale: 0.5) } }",
            CostAnalyses.Cost | CostAnalyses.ResponseSize);

        // act
        var actual = staticPlan.Evaluate(Variables());
        var expected = dynamicPlan.Evaluate(Variables(("limit", new IntValueNode(3))));

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Evaluate_Should_KeepDistinctMemberMetadata_When_StaticFieldCacheHits()
    {
        // arrange
        const string schemaSource = Directives
            + """
              directive @charge(
                payload: Payload = { nested: { value: 1 } } @cost(weight: "0.25")
              ) on FIELD
              input Nested { value: Int @cost(weight: "0.0625") }
              input Payload { nested: Nested @cost(weight: "0.125") }
              interface Item { value: Int }
              type DefaultItem implements Item @cost(weight: "0.25") { value: Int }
              type SpecialItem implements Item @cost(weight: "7") { value: Int }
              interface Node { items(limit: Int = 2): [Item] }
              type A implements Node {
                items(limit: Int = 2 @cost(weight: "0.125")): [DefaultItem]
                  @cost(weight: "2") @listSize(assumedSize: 3)
              }
              type B implements Node {
                items(limit: Int = 2 @cost(weight: "0.125")): [DefaultItem]
                  @cost(weight: "2") @listSize(assumedSize: 3)
              }
              type C implements Node {
                items(limit: Int = 3 @cost(weight: "0.5")): [SpecialItem!]!
                  @cost(weight: "5") @listSize(assumedSize: 4)
              }
              type Query { node: Node }
              """;
        var schema = SchemaParser.Parse(schemaSource);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var plan = Compile(
            snapshot,
            "{ node { items @charge { value } } }",
            CostAnalyses.Cost | CostAnalyses.ResponseSize);
        var a = snapshot.GetFieldDefinition(snapshot.GetObjectTypeIndex("A"), "items");
        var b = snapshot.GetFieldDefinition(snapshot.GetObjectTypeIndex("B"), "items");
        var c = snapshot.GetFieldDefinition(snapshot.GetObjectTypeIndex("C"), "items");

        // act
        var estimate = plan.Evaluate(Variables());

        // assert
        Assert.Equal(snapshot.GetFieldSemanticId(a), snapshot.GetFieldSemanticId(b));
        Assert.NotEqual(snapshot.GetFieldSemanticId(a), snapshot.GetFieldSemanticId(c));
        Assert.Equal(
            [
                BitConverter.DoubleToInt64Bits(6.9375),
                BitConverter.DoubleToInt64Bits(30.0),
                BitConverter.DoubleToInt64Bits(6.0)
            ],
            [
                BitConverter.DoubleToInt64Bits(estimate.FieldCost),
                BitConverter.DoubleToInt64Bits(estimate.TypeCost),
                BitConverter.DoubleToInt64Bits(estimate.MaxResponseSize!.Value)
            ]);
    }

    [Fact]
    public void Evaluate_Should_MatchUnbatchedReference_When_LeafMembersShareSemanticIds()
    {
        // arrange
        const string schemaSource = Directives
            + """
              directive @charge(
                amount: Int @cost(weight: "0.25")
              ) on FIELD
              interface Node { value(limit: Int): [Int] }
              type A implements Node {
                value(limit: Int @cost(weight: "0.125")): [Int]
                  @cost(weight: "0.125") @listSize(assumedSize: 2)
              }
              type B implements Node {
                value(limit: Int @cost(weight: "0.125")): [Int]
                  @cost(weight: "0.125") @listSize(assumedSize: 2)
              }
              type C implements Node {
                value(limit: Int @cost(weight: "0.5")): [Int!]!
                  @cost(weight: "9007199254740992") @listSize(assumedSize: 3)
              }
              type Query { node: Node }
              """;
        const string operationSource =
            """
            query($include: Boolean!) {
              node {
                value(limit: 1) @charge(amount: 2)
                value(limit: 1) @charge(amount: 2) @include(if: $include)
              }
            }
            """;
        var schema = SchemaParser.Parse(schemaSource);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var document = Utf8GraphQLParser.Parse(operationSource);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(
            snapshot,
            document,
            operation,
            "Query");
        var referenceAlgebra = new TupledAlgebra(snapshot);
        var reference = ExactCasesTraversal.Evaluate(
            snapshot,
            fragments,
            tree,
            referenceAlgebra,
            new CaseBudget(snapshot.CaseBudget));
        var plan = CostPlanCompiler.Compile(
            snapshot,
            document,
            operation,
            CostAnalyses.Cost | CostAnalyses.ResponseSize);

        // act
        var actualFalse = plan.Evaluate(Variables(("include", BooleanValueNode.False)));
        var actualTrue = plan.Evaluate(Variables(("include", BooleanValueNode.True)));
        var expectedFalse = reference.Resolve(_ => false);
        var expectedTrue = reference.Resolve(_ => true);
        var actualBound = plan.EvaluateStaticBound();
        var expectedBound = reference.FoldWithJoin(referenceAlgebra.Join);

        // assert
        Assert.Equal(
            Bits(expectedFalse, expectedTrue, expectedBound),
            Bits(actualFalse, actualTrue, actualBound));
    }

    [Fact]
    public void Join_Should_PreserveRawBits_When_IdenticalConstantsContainSpecialValues()
    {
        // arrange
        const double positiveZero = 0.0;
        var negativeZero = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000));
        var payloadNaN = BitConverter.Int64BitsToDouble(0x7ff8000000001234);
        var values =
            new[]
            {
                new CostEstimate(positiveZero, negativeZero, payloadNaN),
                new CostEstimate(payloadNaN, positiveZero, negativeZero)
            };

        // act
        var joined = values
            .Select(static value => PlanArithmetic.Join(value, value, CostAnalyses.Cost | CostAnalyses.ResponseSize))
            .ToArray();
        var canSuppress = values
            .Select(static value => PlanAlgebra.IsBitExactJoinIdempotent(
                value,
                CostAnalyses.Cost | CostAnalyses.ResponseSize))
            .ToArray();

        // assert
        Assert.Equal(Bits(values), Bits(joined));
        Assert.Equal([true, true], canSuppress);
    }

    [Fact]
    public void Evaluate_Should_MatchUnbatchedReference_When_AbstractLeafMembersDependOnVariables()
    {
        // arrange
        const string schemaSource = Directives
            + """
              directive @charge(
                payload: Payload @cost(weight: "0.5")
              ) on FIELD
              input Nested { factor: Int @cost(weight: "0.125") }
              input Payload { nested: Nested @cost(weight: "0.25") }
              interface Node { values(limit: Int, payload: Payload): [Int] }
              type A implements Node {
                values(
                  limit: Int @cost(weight: "0.0625")
                  payload: Payload @cost(weight: "0.375")
                ): [Int] @cost(weight: "0.75") @listSize(slicingArguments: ["limit"])
              }
              type B implements Node {
                values(
                  limit: Int @cost(weight: "0.0625")
                  payload: Payload @cost(weight: "0.375")
                ): [Int] @cost(weight: "0.75") @listSize(slicingArguments: ["limit"])
              }
              type Query {
                nodes(count: Int): [Node]
                  @listSize(assumedSize: 11, slicingArguments: ["count"], sizedFields: ["values"])
              }
              """;
        const string operationSource =
            """
            query(
              $include: Boolean!
              $count: Int!
              $limit: Int!
              $payload: Payload!
              $directive: Payload!
            ) {
              nodes(count: $count) {
                values(limit: $limit, payload: $payload) @charge(payload: $directive)
                values(limit: $limit, payload: $payload)
                  @charge(payload: $directive) @include(if: $include)
              }
            }
            """;
        var schema = SchemaParser.Parse(schemaSource);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var document = Utf8GraphQLParser.Parse(operationSource);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(
            snapshot,
            document,
            operation,
            "Query");
        var payload = new ObjectValueNode(
            new ObjectFieldNode(
                "nested",
                new ObjectValueNode(new ObjectFieldNode("factor", 5))));
        var directive = new ObjectValueNode(
            new ObjectFieldNode(
                "nested",
                new ObjectValueNode(new ObjectFieldNode("factor", 9))));
        var falseVariables = Variables(
            ("include", BooleanValueNode.False),
            ("count", new IntValueNode(3)),
            ("limit", new IntValueNode(7)),
            ("payload", payload),
            ("directive", directive));
        var trueVariables = Variables(
            ("include", BooleanValueNode.True),
            ("count", new IntValueNode(3)),
            ("limit", new IntValueNode(7)),
            ("payload", payload),
            ("directive", directive));
        var plan = CostPlanCompiler.Compile(
            snapshot,
            document,
            operation,
            CostAnalyses.Cost);
        var a = snapshot.GetFieldDefinition(snapshot.GetObjectTypeIndex("A"), "values");
        var b = snapshot.GetFieldDefinition(snapshot.GetObjectTypeIndex("B"), "values");

        // act
        var dynamicReference = ExactCasesTraversal.Evaluate(
            snapshot,
            fragments,
            tree,
            new CostAlgebra(snapshot, falseVariables),
            new CaseBudget(snapshot.CaseBudget));
        var staticAlgebra = new CostAlgebra(snapshot);
        var staticReference = ExactCasesTraversal.Evaluate(
            snapshot,
            fragments,
            tree,
            staticAlgebra,
            new CaseBudget(snapshot.CaseBudget));
        var actualFalse = plan.Evaluate(falseVariables);
        var actualTrue = plan.Evaluate(trueVariables);
        var expectedFalse = dynamicReference.Resolve(_ => false);
        var expectedTrue = dynamicReference.Resolve(_ => true);
        var actualBound = plan.EvaluateStaticBound();
        var expectedBound = staticReference.FoldWithJoin(staticAlgebra.Join);

        // assert
        Assert.Equal(snapshot.GetFieldSemanticId(a), snapshot.GetFieldSemanticId(b));
        Assert.True(plan.DependsOnVariables);
        Assert.Equal(
            Bits(expectedFalse, expectedTrue, expectedBound),
            Bits(actualFalse, actualTrue, actualBound));
    }

    [Fact]
    public void Compile_Should_RemainExact_When_SnapshotIsSharedAndEarlierPlansOutliveCompileChurn()
    {
        // arrange
        var schema = SchemaParser.Parse(
            Directives
            + """
              type Item { value: Int @cost(weight: "2") }
              type Query { items(limit: Int): [Item] @listSize(slicingArguments: ["limit"]) }
              """);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var first = Compile(
            snapshot,
            "query($limit: Int) { items(limit: $limit) { value } }",
            CostAnalyses.Cost | CostAnalyses.ResponseSize);
        var results = new CostEstimate[64];

        // act
        Parallel.For(
            0,
            results.Length,
            index =>
            {
                var plan = Compile(
                    snapshot,
                    "query($limit: Int) { items(limit: $limit) { value } }",
                    CostAnalyses.Cost | CostAnalyses.ResponseSize);
                results[index] = plan.Evaluate(Variables(("limit", new IntValueNode(3))));
            });
        var firstAfterChurn = first.Evaluate(Variables(("limit", new IntValueNode(3))));

        // assert
        Assert.All(results, estimate => Assert.Equal(new CostEstimate(7.0, 4.0, 4.0), estimate));
        Assert.Equal(new CostEstimate(7.0, 4.0, 4.0), firstAfterChurn);
    }

    [Fact]
    public void Evaluate_Should_RemainExact_When_OversizedResponseScratchIsReused()
    {
        // arrange
        const int responseCount = 120;
        var operation = new StringBuilder("query($include: Boolean!) {");

        for (var i = 0; i < responseCount; i++)
        {
            operation.Append(" r").Append(i).Append(": value");
        }

        operation.Append(" ... @include(if: $include) {");

        for (var i = 0; i < responseCount; i++)
        {
            operation.Append(" r").Append(i).Append(": value");
        }

        operation.Append(" } }");
        var schema = SchemaParser.Parse(
            Directives + "type Query { value: Int @cost(weight: \"1\") }");
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var document = Utf8GraphQLParser.Parse(operation.ToString());
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var tree = ConditionTreeExtractor.ExtractOperation(
            snapshot,
            document,
            operationDefinition,
            "Query");
        var scratchLength = FieldGroupAccumulator.GetRequiredScratchLength(tree);
        var first = CostPlanCompiler.Compile(
            snapshot,
            document,
            operationDefinition,
            CostAnalyses.Cost);

        // act
        for (var i = 0; i < 64; i++)
        {
            _ = Compile(snapshot, operation.ToString(), CostAnalyses.Cost);
        }

        var actual =
            new[]
            {
                first.Evaluate(Variables(("include", BooleanValueNode.False))),
                first.Evaluate(Variables(("include", BooleanValueNode.True)))
            };

        // assert
        Assert.True(scratchLength > FieldGroupAccumulator.MaxStackScratchLength);
        Assert.Equal(
            [new CostEstimate(120.0, 1.0, null), new CostEstimate(120.0, 1.0, null)],
            actual);
    }

    [Fact]
    public void Evaluate_Should_AllocateNothing_When_PlanIsWarm()
    {
        // arrange
        var plan = Compile(
            Directives
            + """
              type Item { value: Int @cost(weight: "2") }
              type Query { items(limit: Int!): [Item] @listSize(slicingArguments: ["limit"]) }
              """,
            "query($n: Int!) { items(limit: $n) { value } }");
        var variables = Variables(("n", new IntValueNode(3)));
        _ = plan.Evaluate(variables);

        // act
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1_000; i++)
        {
            _ = plan.Evaluate(variables);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // assert
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void Evaluate_Should_ReturnExactResults_When_PlanAndSnapshotAreSharedAndOptionsAreMutated()
    {
        // arrange
        var schema = SchemaParser.Parse(
            Directives
            + """
              type Item { value: Int }
              type Query { items(limit: Int): [Item] @listSize(slicingArguments: ["limit"]) }
              """);
        var options = new CostEngineOptions { DefaultListSize = 2.0 };
        var snapshot = CostSchemaSnapshot.Create(schema, options);
        var document = Utf8GraphQLParser.Parse("query($n: Int) { items(limit: $n) { value } }");
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var plan = CostPlanCompiler.Compile(
            snapshot,
            document,
            operation,
            CostAnalyses.Cost | CostAnalyses.ResponseSize);
        var estimates = new CostEstimate[64];
        var expected = new CostEstimate[64];
        options.DefaultListSize = 100.0;
        options.CaseBudget = 0;
        var returned = snapshot.Options;
        returned.DefaultListSize = 200.0;
        returned.CaseBudget = 0;

        // act
        Parallel.For(
            0,
            estimates.Length,
            index =>
            {
                if ((index & 1) == 0)
                {
                    var size = index % 8;
                    estimates[index] = plan.Evaluate(Variables(("n", new IntValueNode(size))));
                    expected[index] = new CostEstimate(1.0, size + 1.0, size + 1.0);
                }
                else
                {
                    estimates[index] = plan.Evaluate(Variables());
                    expected[index] = new CostEstimate(1.0, 3.0, 3.0);
                }
            });

        // assert
        Assert.Equal(expected, estimates);
        Assert.Equal(new CostEstimate(1.0, 3.0, 3.0), plan.EvaluateStaticBound());
    }

    private static CostPlan Compile(
        string source,
        string operationSource,
        CostAnalyses analyses = CostAnalyses.Cost)
    {
        var schema = SchemaParser.Parse(source);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var document = Utf8GraphQLParser.Parse(operationSource);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        return CostPlanCompiler.Compile(snapshot, document, operation, analyses);
    }

    private static CostPlan Compile(
        CostSchemaSnapshot snapshot,
        string operationSource,
        CostAnalyses analyses)
    {
        var document = Utf8GraphQLParser.Parse(operationSource);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        return CostPlanCompiler.Compile(snapshot, document, operation, analyses);
    }

    private static ICostVariableValues Variables(params (string Name, IValueNode Value)[] values)
        => new TestVariableValues(values.ToDictionary(pair => pair.Name, pair => pair.Value));

    private static long[] Bits(params CostEstimate[] values)
        => values.SelectMany(
                static value =>
                    new[]
                    {
                        BitConverter.DoubleToInt64Bits(value.FieldCost),
                        BitConverter.DoubleToInt64Bits(value.TypeCost),
                        value.MaxResponseSize is { } size
                            ? BitConverter.DoubleToInt64Bits(size)
                            : long.MinValue
                    })
            .ToArray();

    private sealed class TestVariableValues(
        IReadOnlyDictionary<string, IValueNode> values) : ICostVariableValues
    {
        public bool TryGetValue(string name, out IValueNode? value)
        {
            if (values.TryGetValue(name, out var found))
            {
                value = found;
                return true;
            }

            value = null;
            return false;
        }
    }
}
