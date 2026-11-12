namespace HotChocolate.Fusion.Directives;

public sealed class PolicyRequirementTests
{
    [Fact]
    public void And_Should_CanonicalizeAndMinimize_When_RequirementsContainOverlappingGroups()
    {
        // arrange
        var left = PolicyRequirement.Create(
        [
            ["z", "a"],
            ["a"],
            ["m", "b"],
            ["b", "m"]
        ]);
        var right = PolicyRequirement.Create([["x"], ["a", "b"]]);

        // act
        var result = left.And(right);

        // assert
        string.Join(
            Environment.NewLine,
            result.Groups.Select(static group => string.Join(", ", group))).MatchInlineSnapshot(
            """
            a, b
            a, x
            b, m, x
            """);
    }

    [Fact]
    public void Create_Should_CompareEqual_When_GroupsDifferOnlyInOrder()
    {
        // arrange
        var left = PolicyRequirement.Create([["b", "a"]]);
        var right = PolicyRequirement.Create([["a", "b"]]);

        // act & assert
        Assert.Equal(left, right);
    }

    [Fact]
    public void Covers_Should_ReturnFalse_When_ARequirementHasAnUncoveredAlternative()
    {
        // arrange
        var requirement = PolicyRequirement.Create([["a"], ["b"]]);
        var dependency = PolicyRequirement.Create([["a"]]);

        // act
        var covered = requirement.Covers(dependency);

        // assert
        Assert.False(covered);
    }
}
