using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies the cost engine's public contract compiles and every member is
/// published as a <see cref="NotImplementedException"/> shell, per
/// hc-3-r2l.1, until a follow-up task fills the implementation.
/// </summary>
public class ContractTests
{
    [Fact]
    public void CostSchemaSnapshot_Should_ThrowNotImplemented_When_Members_Are_Invoked()
    {
        // arrange
        var schema = SchemaParser.Parse("type Query { field: String }");
        var options = new CostEngineOptions();
        var snapshot = new CostSchemaSnapshot();

        // act
        void Create() => CostSchemaSnapshot.Create(schema, options);
        void Options() => _ = snapshot.Options;

        // assert
        Assert.Throws<NotImplementedException>(Create);
        Assert.Throws<NotImplementedException>(Options);
    }

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
        var snapshot = new CostSchemaSnapshot();
        var document = Utf8GraphQLParser.Parse("{ field }");
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        void Compile() => CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);

        // assert
        Assert.Throws<NotImplementedException>(Compile);
    }

    [Fact]
    public void CostAlgebra_Should_ThrowNotImplemented_When_Members_Are_Invoked()
    {
        // arrange
        var algebra = new CostAlgebra();

        // act
        void Empty() => _ = algebra.Empty;
        void Field() => algebra.Field(default, default);
        void Combine() => algebra.Combine(default, default);
        void Join() => algebra.Join(default, default);

        // assert
        Assert.Throws<NotImplementedException>(Empty);
        Assert.Throws<NotImplementedException>(Field);
        Assert.Throws<NotImplementedException>(Combine);
        Assert.Throws<NotImplementedException>(Join);
    }

    [Fact]
    public void ResponseSizeAlgebra_Should_ThrowNotImplemented_When_Members_Are_Invoked()
    {
        // arrange
        var algebra = new ResponseSizeAlgebra();

        // act
        void Empty() => _ = algebra.Empty;
        void Field() => algebra.Field(default, default);
        void Combine() => algebra.Combine(default, default);
        void Join() => algebra.Join(default, default);

        // assert
        Assert.Throws<NotImplementedException>(Empty);
        Assert.Throws<NotImplementedException>(Field);
        Assert.Throws<NotImplementedException>(Combine);
        Assert.Throws<NotImplementedException>(Join);
    }
}
