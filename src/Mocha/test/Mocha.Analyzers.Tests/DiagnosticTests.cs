namespace Mocha.Analyzers.Tests;

public class DiagnosticTests
{
    [Fact]
    public async Task MO0001_CommandWithNoHandler_ReportsWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record DeleteOrderCommand(int OrderId) : ICommand;
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0001_QueryWithNoHandler_ReportsWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record GetOrderQuery(int OrderId) : IQuery<string>;
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0002_CommandWithTwoHandlers_ReportsError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record CreateOrderCommand(string Name) : ICommand<int>;

            public class CreateOrderHandlerA : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(1);
            }

            public class CreateOrderHandlerB : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(2);
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NoWarning_CommandWithHandler_NoDiagnostic()
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
    public async Task MO0003_AbstractHandler_ReportsWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record DeleteOrderCommand(int OrderId) : ICommand;

            public abstract class BaseDeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public abstract ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken);
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0002_VoidCommandWithTwoHandlers_ReportsError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record DeleteOrderCommand(int OrderId) : ICommand;

            public class DeleteOrderHandlerA : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }

            public class DeleteOrderHandlerB : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0004_OpenGenericCommand_ReportsInfo()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record GenericCommand<T>(T Value) : ICommand;

            public class GenericCommandHandler<T> : ICommandHandler<GenericCommand<T>>
            {
                public ValueTask HandleAsync(GenericCommand<T> command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0004_OpenGenericQuery_ReportsInfo()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record GenericQuery<T> : IQuery<T>;

            public class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, T>
            {
                public ValueTask<T> HandleAsync(GenericQuery<T> query, CancellationToken cancellationToken)
                    => default!;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
