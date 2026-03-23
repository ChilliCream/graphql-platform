namespace Mocha.Analyzers.Tests;

public class CommandHandlerGeneratorTests
{
    [Fact]
    public async Task Generate_VoidCommandHandler_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record DeleteOrderCommand(int OrderId) : ICommand;

            public class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_CommandWithResponseHandler_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record CreateOrderCommand(string Name) : ICommand<int>;

            public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(42);
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_MultipleCommandHandlers_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record DeleteOrderCommand(int OrderId) : ICommand;

            public class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """,
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record CreateOrderCommand(string Name) : ICommand<int>;

            public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(42);
            }
            """
        ]).MatchMarkdownAsync();
    }
}
