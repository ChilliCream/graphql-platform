namespace HotChocolate.Types.BatchResolvers;

public sealed partial class AbstractParentBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Run_OwnResolver_PerType_When_InterfaceParentHasSameNamedFields(
        DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                characters {
                    name
                    ... on AbstractHuman { friends }
                    ... on AbstractDroid { friends }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        // each concrete type's own resolver ran once, keyed by its own probe records.
        Assert.Equal(2, Probe.Invocations.Count);
        Assert.Contains(Probe.Invocations, i => i.MemberName == "GetFriends" && i.Keys is [1]);
        Assert.Contains(Probe.Invocations, i => i.MemberName == "GetFriends" && i.Keys is [2]);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "characters": [
                  {
                    "name": "Luke",
                    "friends": "human-friend-of-Luke"
                  },
                  {
                    "name": "R2D2",
                    "friends": "droid-friend-of-R2D2"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Run_OwnResolver_PerType_When_UnionParentHasSameNamedFields(
        DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                vehicles {
                    ... on AbstractCar { model spec }
                    ... on AbstractBike { model spec }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, Probe.Invocations.Count);
        Assert.Contains(Probe.Invocations, i => i.MemberName == "GetSpec" && i.Keys is [1]);
        Assert.Contains(Probe.Invocations, i => i.MemberName == "GetSpec" && i.Keys is [2]);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "vehicles": [
                  {
                    "model": "Model S",
                    "spec": "car-spec-of-Model S"
                  },
                  {
                    "model": "BMX",
                    "spec": "bike-spec-of-BMX"
                  }
                ]
              }
            }
            """);
    }
}
