using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class OperationPlanStepTests : FusionTestBase
{
    [Fact]
    public void DependsOn_Should_ReturnTrue_When_OtherStepListsStepAsDependent()
    {
        // arrange
        var steps = CreateSteps(2, (1, [2]));

        // act
        var dependsOn = Step(steps, 2).DependsOn(Step(steps, 1), steps);
        var reverse = Step(steps, 1).DependsOn(Step(steps, 2), steps);

        // assert
        Assert.True(dependsOn);
        Assert.False(reverse);
    }

    [Fact]
    public void DependsOn_Should_ReturnTrue_When_StepIsReachableThroughDependents()
    {
        // arrange
        var steps = CreateSteps(4, (1, [2]), (2, [3]), (3, [4]));

        // act
        var dependsOn = Step(steps, 4).DependsOn(Step(steps, 1), steps);

        // assert
        Assert.True(dependsOn);
    }

    [Fact]
    public void DependsOn_Should_ReturnFalse_When_StepIsNotReachableThroughDependents()
    {
        // arrange
        var steps = CreateSteps(4, (1, [2]), (3, [4]));

        // act
        var dependsOn = Step(steps, 4).DependsOn(Step(steps, 1), steps);

        // assert
        Assert.False(dependsOn);
    }

    [Fact]
    public void DependsOn_Should_ReturnFalse_When_DependentsFormCycleWithoutReachingStep()
    {
        // arrange
        var steps = CreateSteps(3, (1, [2]), (2, [1]));

        // act
        var dependsOn = Step(steps, 3).DependsOn(Step(steps, 1), steps);

        // assert
        Assert.False(dependsOn);
    }

    [Theory]
    [InlineData(70)]
    [InlineData(2100)]
    public void DependsOn_Should_ReturnTrue_When_DependencyChainIsLong(int length)
    {
        // arrange
        var edges = Enumerable.Range(1, length - 1)
            .Select(id => (id, new[] { id + 1 }))
            .ToArray();
        var steps = CreateSteps(length, edges);

        // act
        var dependsOn = Step(steps, length).DependsOn(Step(steps, 1), steps);
        var reverse = Step(steps, 1).DependsOn(Step(steps, length), steps);

        // assert
        Assert.True(dependsOn);
        Assert.False(reverse);
    }

    private static OperationPlanStep Step(ImmutableList<PlanStep> steps, int id)
        => (OperationPlanStep)steps[id - 1];

    private static ImmutableList<PlanStep> CreateSteps(
        int count,
        params (int Id, int[] Dependents)[] edges)
    {
        var schema = ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              field: Int
            }
            """);

        var definition = Utf8GraphQLParser
            .Parse("{ field }")
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var type = schema.Types["Query"];
        var dependents = edges.ToDictionary(e => e.Id, e => e.Dependents);

        var steps = ImmutableList.CreateBuilder<PlanStep>();

        for (var id = 1; id <= count; id++)
        {
            steps.Add(
                new OperationPlanStep
                {
                    Id = id,
                    Definition = definition,
                    Type = type,
                    RootSelectionSetId = 1,
                    SelectionSets = [1],
                    SchemaName = "a",
                    Target = SelectionPath.Root,
                    Source = SelectionPath.Root,
                    Dependents = dependents.TryGetValue(id, out var ids)
                        ? [.. ids]
                        : []
                });
        }

        return steps.ToImmutable();
    }
}
