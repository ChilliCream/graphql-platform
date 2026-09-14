namespace HotChocolate.Types.BatchResolvers;

public sealed partial class InterfaceBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = Declaration.NotApplicable(AttributeNotApplicableReason),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Resolve_When_DeclaredOnInterfaceAndInheritedByObjectType(
        DeclarationStyle style)
    {
        // arrange
        if (GetNotApplicableReason(style) is not null)
        {
            return;
        }

        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name greeting } }", TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Bind_Argument_When_DeclaredOnInterface(DeclarationStyle style)
    {
        // arrange
        if (GetNotApplicableReason(style) is not null)
        {
            return;
        }

        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            { users { name greetingWithArgument(prefix: "Hi") } }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greetingWithArgument": "Hi, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greetingWithArgument": "Hi, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greetingWithArgument": "Hi, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Inject_Service_When_DeclaredOnInterface(DeclarationStyle style)
    {
        // arrange
        if (GetNotApplicableReason(style) is not null)
        {
            return;
        }

        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name greetingWithService } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greetingWithService": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greetingWithService": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greetingWithService": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }
}
