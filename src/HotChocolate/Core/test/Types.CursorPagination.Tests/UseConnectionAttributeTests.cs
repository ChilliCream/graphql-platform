using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class UseConnectionAttributeTests
{
    [Fact]
    public async Task UseConnectionAttribute_Validates_Max_Page_Size()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<UseConnectionQueryType>()
            .ExecuteRequestAsync(
                """
                {
                  foos(first: 3)
                }
                """);

        AssertErrorCode(result, ErrorCodes.Paging.MaxPaginationItems);
    }

    [Fact]
    public async Task UseConnectionAttribute_Validates_Boundaries()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<UseConnectionQueryType>()
            .ExecuteRequestAsync(
                """
                {
                  foos
                }
                """);

        AssertErrorCode(result, ErrorCodes.Paging.NoPagingBoundaries);
    }

    [Fact]
    public async Task UseConnectionAttribute_Validates_First_When_Backward_Paging_Is_Disabled()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<UseConnectionQueryType>()
            .ExecuteRequestAsync(
                """
                {
                  foosNoBackward
                }
                """);

        AssertErrorCode(result, ErrorCodes.Paging.FirstValueNotSet);
    }

    [Fact]
    public async Task UseConnectionAttribute_Clamps_Default_Page_Size_To_Max_Page_Size()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<UseConnectionQueryType>()
            .ExecuteRequestAsync(
                """
                {
                  foosClampedDefault
                }
                """);

        var operationResult = result.ExpectOperationResult();

        Assert.True(
            operationResult.Errors is null || operationResult.Errors.Count == 0,
            $"Expected no errors but got: {operationResult.ToJson()}");

        using var document = JsonDocument.Parse(operationResult.ToJson());
        Assert.Equal(
            2,
            document
                .RootElement
                .GetProperty("data")
                .GetProperty("foosClampedDefault")
                .GetInt32());
    }

    public class UseConnectionQueryType : ObjectType<UseConnectionQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<UseConnectionQuery> descriptor)
        {
            descriptor
                .Field(t => t.GetFoos())
                .AddPagingArguments();

            descriptor
                .Field(t => t.GetFoosNoBackward())
                .AddPagingArguments();

            descriptor
                .Field(t => t.GetFoosClampedDefault(default!))
                .AddPagingArguments();
        }
    }

    public class UseConnectionQuery
    {
        [UseConnection(MaxPageSize = 2, RequirePagingBoundaries = true)]
        public string GetFoos()
            => throw new InvalidOperationException("Resolver should not be called.");

        [UseConnection(AllowBackwardPagination = false, RequirePagingBoundaries = true)]
        public string GetFoosNoBackward()
            => throw new InvalidOperationException("Resolver should not be called.");

        [UseConnection(DefaultPageSize = 3, MaxPageSize = 2)]
        public int GetFoosClampedDefault(IResolverContext context)
        {
            var pagingArgs =
                context.GetLocalState<CursorPagingArguments>(WellKnownContextData.PagingArguments);
            return pagingArgs.First ?? -1;
        }
    }

    private static void AssertErrorCode(
        IExecutionResult executionResult,
        string code)
    {
        var operationResult = executionResult.ExpectOperationResult();
        var error = Assert.Single(operationResult.Errors!);

        Assert.True(
            error.Code == code,
            $"Expected code {code} but got {error.Code}. Message: {error.Message}. Result: {operationResult.ToJson()}");
    }
}
