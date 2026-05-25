namespace Mocha.Analyzers.Tests;

public class WarmUpGeneratorTests
{
    [Fact]
    public async Task Generate_WarmUpMethod_WithAllHandlerTypes_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            // Void command
            public record DeleteItemCommand(int Id) : ICommand;

            // Command with response
            public record CreateItemCommand(string Name) : ICommand<int>;

            // Query
            public record GetItemQuery(int Id) : IQuery<ItemDto>;
            public record ItemDto(int Id, string Name);

            // Notification with single handler
            public record ItemCreated(int Id) : INotification;

            // Handlers
            public class DeleteItemHandler : ICommandHandler<DeleteItemCommand>
            {
                public ValueTask HandleAsync(DeleteItemCommand command, CancellationToken cancellationToken)
                    => default;
            }

            public class CreateItemHandler : ICommandHandler<CreateItemCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateItemCommand command, CancellationToken cancellationToken)
                    => new(1);
            }

            public class GetItemHandler : IQueryHandler<GetItemQuery, ItemDto>
            {
                public ValueTask<ItemDto> HandleAsync(GetItemQuery query, CancellationToken cancellationToken)
                    => new(new ItemDto(1, "Test"));
            }

            public class ItemCreatedHandler : INotificationHandler<ItemCreated>
            {
                public ValueTask HandleAsync(ItemCreated notification, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
