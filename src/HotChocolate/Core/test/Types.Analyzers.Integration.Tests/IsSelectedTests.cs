using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class IsSelectedTests
{
    [Fact]
    public async Task IsSelected_Should_ReturnTrue_When_FieldIsSelected()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddGlobalObjectIdentification()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ isSelectedTest { name wasNameSelected } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "isSelectedTest": {
                  "name": "test",
                  "wasNameSelected": true
                }
              }
            }
            """);
    }

    [Fact]
    public async Task IsSelected_Should_ReturnFalse_When_FieldIsNotSelected()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddGlobalObjectIdentification()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ isSelectedTest { description wasNameSelected } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "isSelectedTest": {
                  "description": "desc",
                  "wasNameSelected": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task IsSelected_NodeResolver_Should_ReturnTrue_When_FieldIsSelected()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddGlobalObjectIdentification()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var id = Convert.ToBase64String("IsSelectedNode:1"u8);

        // act
        var result = await executor.ExecuteAsync(
            $$"""{ node(id: "{{id}}") { ... on IsSelectedNode { name wasNameSelected } } }""",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "name": "test",
                  "wasNameSelected": true
                }
              }
            }
            """);
    }

    [Fact]
    public async Task IsSelected_NodeResolver_Should_ReturnFalse_When_FieldIsNotSelected()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddGlobalObjectIdentification()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var id = Convert.ToBase64String("IsSelectedNode:1"u8);

        // act
        var result = await executor.ExecuteAsync(
            $$"""{ node(id: "{{id}}") { ... on IsSelectedNode { description wasNameSelected } } }""",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "description": "desc",
                  "wasNameSelected": false
                }
              }
            }
            """);
    }
}
