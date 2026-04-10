namespace Mocha.Analyzers.Tests;

public class QueryHandlerGeneratorTests
{
    [Fact]
    public async Task Generate_QueryHandler_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record GetUserQuery(int Id) : IQuery<UserDto>;

            public record UserDto(int Id, string Name);

            public class GetUserHandler : IQueryHandler<GetUserQuery, UserDto>
            {
                public ValueTask<UserDto> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
                    => new(new UserDto(query.Id, "Test"));
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_MultipleQueryHandlers_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record GetUserQuery(int Id) : IQuery<UserDto>;
            public record UserDto(int Id, string Name);

            public class GetUserHandler : IQueryHandler<GetUserQuery, UserDto>
            {
                public ValueTask<UserDto> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
                    => new(new UserDto(query.Id, "Test"));
            }
            """,
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record GetOrderQuery(int Id) : IQuery<OrderDto>;
            public record OrderDto(int Id, string Status);

            public class GetOrderHandler : IQueryHandler<GetOrderQuery, OrderDto>
            {
                public ValueTask<OrderDto> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
                    => new(new OrderDto(query.Id, "Pending"));
            }
            """
        ]).MatchMarkdownAsync();
    }
}
