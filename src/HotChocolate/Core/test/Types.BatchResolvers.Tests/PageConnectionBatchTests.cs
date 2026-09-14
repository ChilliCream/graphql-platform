namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves the <c>PageConnection&lt;T&gt;</c> paging model (hc-0-jyk.2 generated default/selection
/// binding) can carry a batch-resolved <c>PagingArguments</c> parameter, kept in a family of its
/// own: mixing a classic <c>UsePaging</c> connection with <c>PageConnection&lt;T&gt;</c> in the
/// same schema collides on the <c>PageInfo</c> type name (HC0065, adam-1's 2026-09-14 planner
/// note) because the two models resolve to the same GraphQL type name for a structurally
/// different C# type. One paging model per schema, per that ruling.
/// </summary>
public sealed partial class PageConnectionBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task UseConnection_Should_Map_PagingArguments_When_ReturnTypeIsPageConnection(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                brands {
                    name
                    small: products(first: 1) { nodes { name } }
                    large: products(first: 2) { nodes { name } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, Probe.Invocations.Count);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        },
                        {
                          "name": "Brand 1 Product 2"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        },
                        {
                          "name": "Brand 2 Product 2"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }
}
