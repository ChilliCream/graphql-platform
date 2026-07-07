using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class FlagsEnumMethodBindingTests
{
    [Fact]
    public async Task FlagsEnum_Should_CoerceToFlagsObject_When_ReturnedFromPropertyBoundField()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .ModifyOptions(o => o.EnableFlagEnums = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ animal { kind { isDog isCat } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "animal": {
                  "kind": {
                    "isDog": true,
                    "isCat": true
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task FlagsEnum_Should_CoerceToFlagsObject_When_ReturnedFromMethodBoundField()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .ModifyOptions(o => o.EnableFlagEnums = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ zoo { animals { isDog isCat } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "zoo": {
                  "animals": {
                    "isDog": true,
                    "isCat": true
                  }
                }
              }
            }
            """);
    }
}
