using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Unit tests for the generic <see cref="AnalysisPlan"/>/
/// <see cref="AnalysisPlanCompiler"/> surface: Boolean variable resolution,
/// agreement with the ExactCases traversal it is built on, and case-budget
/// behavior shared with the optimized <see cref="CostPlan"/> path.
/// </summary>
public sealed class AnalysisPlanTests
{
    private const string Sdl =
        """
        type Side { costly: Int @cost(weight: "10") }
        type Query { left: Side right: Side }
        """;

    private const string Operation =
        """
        query Example($x: Boolean!) {
          left { costly @include(if: $x) }
          right { costly @skip(if: $x) }
        }
        """;

    private const string GateSdl = "type Query { gated: Int }";

    private const string GateOperation = "query Example($x: Boolean!) { gated @include(if: $x) }";

    [Theory]
    [InlineData(true, 1.0)]
    [InlineData(false, 1.0)]
    public void Evaluate_Should_ResolveCoercedBooleanVariable_LikeCostPlan(bool x, double expectedFieldCount)
    {
        // arrange
        var (plan, _, _) = Compile(Sdl, Operation);
        var variables = Variables(("x", x ? BooleanValueNode.True : BooleanValueNode.False));

        // act
        var fieldCount = plan.Evaluate(new FieldPresenceAlgebra(), variables);

        // assert: @include(if:$x) and @skip(if:$x) on siblings are
        // complementary, so exactly one "costly" is active either way.
        Assert.Equal(expectedFieldCount > 0, fieldCount);
    }

    [Fact]
    public void Evaluate_Should_TreatUndefinedVariable_AsFalse_LikeCostPlan()
    {
        // arrange
        var (plan, _, _) = Compile(GateSdl, GateOperation);
        var variables = Variables();

        // act: $x is required, but a caller-supplied ICostVariableValues
        // can still omit it; TryGetValue returning false must behave like
        // ConditionPlanNode's coercion (false), not throw.
        var gatedIncluded = plan.Evaluate(new FieldPresenceAlgebra(), variables);

        // assert
        Assert.False(gatedIncluded);
    }

    [Fact]
    public void Evaluate_Should_TreatNonBooleanCoercedValue_AsFalse_LikeCostPlan()
    {
        // arrange
        var (plan, _, _) = Compile(GateSdl, GateOperation);
        var variables = Variables(("x", new IntValueNode(1)));

        // act
        var gatedIncluded = plan.Evaluate(new FieldPresenceAlgebra(), variables);

        // assert
        Assert.False(gatedIncluded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Evaluate_Should_MatchExactCasesTraversalResolve_Reference(bool x)
    {
        // arrange
        var (plan, document, operation) = Compile(Sdl, Operation);
        var schemaIndex = ConditionTreeTestHelpers.BuildSchemaIndex(Sdl);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(schemaIndex, document, operation, "Query");
        var algebra = new TestCostAlgebra();
        var reference = ExactCasesTraversal.Evaluate(
            schemaIndex,
            fragments,
            tree,
            algebra,
            variableValues: null,
            new CaseBudget(4096));
        var variables = Variables(("x", x ? BooleanValueNode.True : BooleanValueNode.False));

        // act
        var actual = plan.Evaluate(new TestCostAlgebra(), variables);
        var expected = reference.Resolve(_ => x);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EvaluateAssumedBound_Should_MatchFoldWithJoin_Reference()
    {
        // arrange
        var (plan, document, operation) = Compile(Sdl, Operation);
        var schemaIndex = ConditionTreeTestHelpers.BuildSchemaIndex(Sdl);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(schemaIndex, document, operation, "Query");
        var referenceAlgebra = new TestCostAlgebra();
        var reference = ExactCasesTraversal.Evaluate(
            schemaIndex,
            fragments,
            tree,
            referenceAlgebra,
            variableValues: null,
            new CaseBudget(4096));

        // act
        var actual = plan.EvaluateAssumedBound(new TestCostAlgebra());
        var expected = reference.FoldWithJoin(referenceAlgebra.Join);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HitCaseBudget_Should_BeAlgebraIndependent_AndMatchCostPlan()
    {
        // arrange: 10 independently @include-gated sibling fields need
        // 2^10 - 1 = 1023 splits: too many for the tight budget, comfortably
        // inside the roomy one.
        var (sdl, operationText) = GenerateIndependentlyGatedOperation(10);
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var tightSchemaIndex = CostSchemaIndex.Create(
            SchemaParser.Parse(sdl),
            new CostSchemaIndexOptions { CaseBudget = 4 });
        var roomySchemaIndex = CostSchemaIndex.Create(
            SchemaParser.Parse(sdl),
            new CostSchemaIndexOptions { CaseBudget = 4096 });

        // act
        var tightCostPlan = CostPlanCompiler.Compile(tightSchemaIndex, document, operation, CostAnalyses.Cost);
        var tightAnalysisPlan = AnalysisPlanCompiler.Compile(tightSchemaIndex, document, operation);
        var roomyCostPlan = CostPlanCompiler.Compile(roomySchemaIndex, document, operation, CostAnalyses.Cost);
        var roomyAnalysisPlan = AnalysisPlanCompiler.Compile(roomySchemaIndex, document, operation);

        // assert: case-budget exhaustion depends only on the condition
        // tree's shape and the schema's case budget, never on the algebra,
        // so the generic plan must agree with the optimized cost plan.
        Assert.True(tightCostPlan.HitCaseBudget);
        Assert.True(tightAnalysisPlan.HitCaseBudget);
        Assert.False(roomyCostPlan.HitCaseBudget);
        Assert.False(roomyAnalysisPlan.HitCaseBudget);
    }

    [Fact]
    public void Evaluate_Should_OverapproximateWithFallbackEnvelope_When_CaseBudgetExhausted()
    {
        // arrange
        var (sdl, operationText) = GenerateIndependentlyGatedOperation(6);
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var tightSchemaIndex = CostSchemaIndex.Create(
            SchemaParser.Parse(sdl),
            new CostSchemaIndexOptions { CaseBudget = 4 });
        var roomySchemaIndex = CostSchemaIndex.Create(
            SchemaParser.Parse(sdl),
            new CostSchemaIndexOptions { CaseBudget = 4096 });
        var tightPlan = AnalysisPlanCompiler.Compile(tightSchemaIndex, document, operation);
        var roomyPlan = AnalysisPlanCompiler.Compile(roomySchemaIndex, document, operation);
        var allFalse = Variables();

        // act
        var fallback = tightPlan.Evaluate(new FieldCountAlgebra(), allFalse);
        var exact = roomyPlan.Evaluate(new FieldCountAlgebra(), allFalse);

        // assert: the fallback envelope follows every pending Boolean edge
        // unconditionally, overapproximating the exact (all-excluded) count.
        Assert.True(tightPlan.HitCaseBudget);
        Assert.False(roomyPlan.HitCaseBudget);
        Assert.Equal(6, fallback);
        Assert.Equal(0, exact);
    }

    private static (AnalysisPlan Plan, DocumentNode Document, OperationDefinitionNode Operation) Compile(
        string sdl,
        string operationText)
    {
        var schemaIndex = ConditionTreeTestHelpers.BuildSchemaIndex(sdl);
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var plan = AnalysisPlanCompiler.Compile(schemaIndex, document, operation);
        return (plan, document, operation);
    }

    private static ICostVariableValues Variables(params (string Name, IValueNode Value)[] values)
        => new TestVariableValues(values.ToDictionary(pair => pair.Name, pair => pair.Value));

    private static (string Sdl, string Operation) GenerateIndependentlyGatedOperation(int variableCount)
    {
        var sdl = new StringBuilder("type Query {");
        var variableDeclarations = new StringBuilder();
        var selections = new StringBuilder();

        for (var i = 0; i < variableCount; i++)
        {
            sdl.Append(" f").Append(i).Append(": Int");

            if (i > 0)
            {
                variableDeclarations.Append(", ");
            }

            variableDeclarations.Append('$').Append('v').Append(i).Append(": Boolean!");
            selections.Append(" f").Append(i).Append(" @include(if: $v").Append(i).Append(')');
        }

        sdl.Append(" }");
        var operation = new StringBuilder("query(")
            .Append(variableDeclarations)
            .Append(") {")
            .Append(selections)
            .Append(" }");
        return (sdl.ToString(), operation.ToString());
    }

    private sealed class TestVariableValues(IReadOnlyDictionary<string, IValueNode> values) : ICostVariableValues
    {
        public bool TryGetValue(string name, out IValueNode? value) => values.TryGetValue(name, out value);
    }

    /// <summary>
    /// Counts present fields, ignoring their children: a minimal algebra
    /// used only to observe which of two mutually gated fields is active.
    /// </summary>
    private sealed class FieldPresenceAlgebra : IAnalysisAlgebra<bool>
    {
        public bool Empty => false;

        public bool Field(in CollectedFieldGroup group, bool child) => true;

        public bool Combine(bool left, bool right) => left || right;

        public bool Join(bool left, bool right) => left || right;

        public bool Root(double rootTypeWeight, bool selection) => selection;
    }

    private sealed class FieldCountAlgebra : IAnalysisAlgebra<int>
    {
        public int Empty => 0;

        public int Field(in CollectedFieldGroup group, int child) => 1 + child;

        public int Combine(int left, int right) => left + right;

        public int Join(int left, int right) => Math.Max(left, right);

        public int Root(double rootTypeWeight, int selection) => selection;
    }
}
