using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies the cost engine's public contract compiles and every remaining
/// unimplemented member is published as a <see cref="NotImplementedException"/>
/// shell, per hc-3-r2l.1, until a follow-up task fills it in.
/// <see cref="CostSchemaSnapshot"/> is implemented (hc-3-r2l.2) and covered
/// by its own Snapshot test suite instead; <see cref="CostAlgebra"/> is
/// implemented and covered by its own CostAlgebra test suite instead.
/// </summary>
public class ContractTests
{
    [Fact]
    public void CostPlan_Should_ThrowNotImplemented_When_Members_Are_Invoked()
    {
        // arrange
        var plan = new CostPlan();

        // act
        void Analyses() => _ = plan.Analyses;
        void DependsOnVariables() => _ = plan.DependsOnVariables;
        void HitCaseBudget() => _ = plan.HitCaseBudget;
        void Evaluate() => plan.Evaluate(null!);
        void EvaluateStaticBound() => plan.EvaluateStaticBound();

        // assert
        Assert.Throws<NotImplementedException>(Analyses);
        Assert.Throws<NotImplementedException>(DependsOnVariables);
        Assert.Throws<NotImplementedException>(HitCaseBudget);
        Assert.Throws<NotImplementedException>(Evaluate);
        Assert.Throws<NotImplementedException>(EvaluateStaticBound);
    }

    [Fact]
    public void CostPlanCompiler_Compile_Should_ThrowNotImplemented_When_Called()
    {
        // arrange
        var schema = SchemaParser.Parse("type Query { field: String }");
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var document = Utf8GraphQLParser.Parse("{ field }");
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        void Compile() => CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);

        // assert
        Assert.Throws<NotImplementedException>(Compile);
    }
}
