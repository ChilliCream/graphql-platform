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

    [Fact]
    public async Task MO0006_OpenGenericHandlerWithConcreteMessage_ReportsInfo()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record MyCommand : ICommand;

            public class GenericHandler<T> : ICommandHandler<MyCommand>
            {
                public ValueTask HandleAsync(MyCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0005_CommandAndNotificationHandler_ReportsError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record DoSomethingCommand : ICommand;
            public record SomethingHappened : INotification;

            public class MultiHandler
                : ICommandHandler<DoSomethingCommand>
                , INotificationHandler<SomethingHappened>
            {
                public ValueTask HandleAsync(DoSomethingCommand command, CancellationToken cancellationToken)
                    => default;

                public ValueTask HandleAsync(SomethingHappened notification, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0005_CommandAndQueryHandler_ReportsError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record DoSomethingCommand : ICommand;
            public record GetSomethingQuery : IQuery<string>;

            public class MultiHandler
                : ICommandHandler<DoSomethingCommand>
                , IQueryHandler<GetSomethingQuery, string>
            {
                public ValueTask HandleAsync(DoSomethingCommand command, CancellationToken cancellationToken)
                    => default;

                public ValueTask<string> HandleAsync(GetSomethingQuery query, CancellationToken cancellationToken)
                    => new("result");
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NoWarning_SingleHandlerInterface_NoDiagnostic()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record SomethingHappened : INotification;

            public class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
            {
                public ValueTask HandleAsync(SomethingHappened notification, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MO0006_OpenGenericNotificationHandler_ReportsInfo()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record MyNotif : INotification;

            public class GenericNotif<T> : INotificationHandler<MyNotif>
            {
                public ValueTask HandleAsync(MyNotif notification, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NoWarning_ClosedConcreteHandler_NoDiagnostic()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record MyCommand : ICommand;

            public class ConcreteHandler : ICommandHandler<MyCommand>
            {
                public ValueTask HandleAsync(MyCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NoWarning_UnrelatedGenericClass_NoDiagnostic()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public class UnrelatedGeneric<T>
            {
                public T? Value { get; set; }
            }
            """
        ]).MatchMarkdownAsync();
    }
}
