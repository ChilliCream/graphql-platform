namespace Mocha.Analyzers.Tests;

public class MixedHandlerGeneratorTests
{
    [Fact]
    public async Task Generate_AllHandlerTypes_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            // Commands
            public record CreateOrderCommand(string Name) : ICommand<int>;
            public record DeleteOrderCommand(int OrderId) : ICommand;

            // Queries
            public record GetUserQuery(int Id) : IQuery<UserDto>;
            public record UserDto(int Id, string Name);

            // Notifications
            public record OrderCreated(int OrderId) : INotification;

            // Handlers
            public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(1);
            }

            public class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }

            public class GetUserHandler : IQueryHandler<GetUserQuery, UserDto>
            {
                public ValueTask<UserDto> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
                    => new(new UserDto(1, "Test"));
            }

            public class OrderCreatedEmailHandler : INotificationHandler<OrderCreated>
            {
                public ValueTask HandleAsync(OrderCreated notification, CancellationToken cancellationToken)
                    => default;
            }

            public class OrderCreatedStatsHandler : INotificationHandler<OrderCreated>
            {
                public ValueTask HandleAsync(OrderCreated notification, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_NoHandlers_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            namespace TestApp;

            public class SomeClass
            {
                public void DoStuff() { }
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_HandlersInDifferentNamespaces_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp.Orders;

            public record CreateOrderCommand(string Name) : ICommand<int>;

            public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(1);
            }
            """,
            """
            using Mocha.Mediator;

            namespace TestApp.Users;

            public record GetUserQuery(int Id) : IQuery<UserDto>;
            public record UserDto(int Id, string Name);

            public class GetUserHandler : IQueryHandler<GetUserQuery, UserDto>
            {
                public ValueTask<UserDto> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
                    => new(new UserDto(1, "Test"));
            }
            """
        ]).MatchMarkdownAsync();
    }
}
