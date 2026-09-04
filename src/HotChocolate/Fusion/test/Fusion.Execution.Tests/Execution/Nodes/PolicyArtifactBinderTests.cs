using System.Collections.Immutable;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class PolicyArtifactBinderTests
{
    [Fact]
    public void GetRequirements_Should_ConcatenateMemberRequirements_When_Batch()
    {
        // arrange
        // Nested-defer requirement routing can produce this batch shape. repo-n4r must pin that planner route when it lands.
        var first = CreateRequirement("first");
        var second = CreateRequirement("second");
        var third = CreateRequirement("third");
        var batch = new OperationBatchExecutionNode(
            1,
            [
                CreateOperation(2, [first, second]),
                CreateOperation(3, [third])
            ]);

        // act
        var requirements = PolicyArtifactBinder.GetBatchRequirements(batch.Operations.ToArray());

        // assert
        Assert.Equal([first, second, third], requirements);
    }

    [Fact]
    public void ProducesCandidate_Should_RequireCompiledSelection_When_ResultChildIsPruned()
    {
        // arrange
        var candidate = SelectionPath.Parse("$.product.rating");
        var prunedRoot = CreateOperation(
            id: 4,
            source: "query { product { id } }",
            target: SelectionPath.Root,
            sourcePath: SelectionPath.Root,
            ResultSelectionSet.Create(Utf8GraphQLParser.Syntax.ParseSelectionSet("{ product }")));
        var lookup = CreateOperation(
            id: 5,
            source: "query { product { rating } }",
            target: SelectionPath.Parse("$.product"),
            sourcePath: SelectionPath.Parse("$.product"),
            ResultSelectionSet.Create(Utf8GraphQLParser.Syntax.ParseSelectionSet("{ rating }")));

        // act
        var produces = new[]
        {
            PolicyArtifactBinder.ProducesCandidate(prunedRoot, candidate),
            PolicyArtifactBinder.ProducesCandidate(lookup, candidate)
        };

        // assert
        Assert.Equal([false, true], produces);
    }

    [Fact]
    public void ValidatePolicyTopology_Should_RejectAmbiguousGuardedProducers_When_TargetHasNoCurrentOccurrences()
    {
        // arrange
        var first = CreateOperation(
            id: 6,
            source: "query { first }",
            target: SelectionPath.Root,
            sourcePath: SelectionPath.Root,
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ first }")));
        var second = CreateOperation(
            id: 7,
            source: "query { second }",
            target: SelectionPath.Root,
            sourcePath: SelectionPath.Root,
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ second }")));
        var target = new PolicyExecutionTarget
        {
            Occurrences =
            [
                new PolicyOccurrenceReference
                {
                    PlanPart = 1,
                    SelectionSetId = 1,
                    SelectionId = 1,
                    OccurrenceOrdinal = 0,
                    ApplicationOrdinal = 0,
                    Facet = PolicyOccurrenceFacet.ResidualEvaluation
                }
            ],
            Kind = PolicyTargetKind.Field,
            Path = SelectionPath.Parse("$.guarded"),
            TypeName = "Query",
            Policies = []
        };
        var policy = new PolicyExecutionNode(8, [target], []);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyArtifactBinder.ValidatePolicyTopology([first, second, policy], planPart: 0));

        // assert
        Assert.Equal(
            "A policy execution node has ambiguous guarded producers at the same target depth.",
            exception.Message);
    }

    private static OperationRequirement CreateRequirement(string map)
        => new(
            "requirement",
            Utf8GraphQLParser.Syntax.ParseTypeReference("String"),
            SelectionPath.Parse("$.product"),
            new FieldSelectionMapParser(map).Parse());

    private static SingleOperationDefinition CreateOperation(
        int id,
        OperationRequirement[] requirements)
        => CreateOperation(
            id,
            "query { product { id } }",
            SelectionPath.Parse("$.product"),
            SelectionPath.Parse("$.product"),
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
            requirements);

    private static OperationExecutionNode CreateOperation(
        int id,
        string source,
        SelectionPath target,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet)
    {
        var sourceText = Encoding.UTF8.GetBytes(source);

        return new OperationExecutionNode(
            id,
            new OperationSourceText(
                $"Operation_{id}",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            lookupTypeName: null,
            schemaName: "a",
            target,
            sourcePath,
            requirements: [],
            forwardedVariables: [],
            resultSelectionSet,
            conditions: [],
            requiresFileUpload: false);
    }

    private static SingleOperationDefinition CreateOperation(
        int id,
        string source,
        SelectionPath target,
        SelectionPath sourcePath,
        ResultSelectionSet resultSelectionSet,
        OperationRequirement[] requirements)
    {
        var sourceText = Encoding.UTF8.GetBytes(source);

        return new SingleOperationDefinition(
            id,
            new OperationSourceText(
                $"Operation_{id}",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            lookupTypeName: null,
            schemaName: "a",
            target,
            sourcePath,
            requirements,
            forwardedVariables: [],
            resultSelectionSet,
            conditions: [],
            requiresFileUpload: false);
    }
}
