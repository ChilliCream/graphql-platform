namespace Mocha.Analyzers.Tests;

public class PartialClassHandlerTests
{
    [Fact]
    public async Task Generate_PartialClassHandler_MatchesSnapshot()
    {
        // The handler interface is declared on one partial declaration,
        // while the method body is in a separate partial declaration.
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record CreateOrderCommand(string Name) : ICommand<int>;

            public partial class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>
            {
            }
            """,
            """
            using Mocha.Mediator;

            namespace TestApp;

            public partial class CreateOrderHandler
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(42);
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_PartialVoidCommandHandler_AcrossFiles_MatchesSnapshot()
    {
        // Item 9: Partial void command handler split across two syntax trees
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record ProcessOrderCommand(int OrderId) : ICommand;

            public partial class ProcessOrderHandler : ICommandHandler<ProcessOrderCommand>
            {
                public ValueTask HandleAsync(ProcessOrderCommand command, CancellationToken cancellationToken)
                {
                    Process(command.OrderId);
                    return default;
                }
            }
            """,
            """
            namespace TestApp;

            public partial class ProcessOrderHandler
            {
                private void Process(int orderId)
                {
                    // Implementation
                }
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitSingleHandlerInitializer_When_PartialHandlerRepeatsInterfaceOnBothParts()
    {
        // The generator must collapse the two per-part handler infos into one registration and
        // one initializer method, and ValidateMessageHandlerPairing must dedupe by handler type
        // name before counting, so no MO0002 is emitted for this single handler restated
        // across both partial declaration parts.
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record CreateOrderCommand(string Name) : ICommand<int>;

            public partial class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(42);
            }
            """,
            """
            using Mocha.Mediator;

            namespace TestApp;

            public partial class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>;
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitSingleNotificationInitializer_When_PartialNotificationHandlerRepeatsInterfaceOnBothParts()
    {
        // The generator must collapse the two per-part notification handler infos into one
        // registration and one initializer method.
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record OrderCreated(int OrderId) : INotification;

            public partial class OrderCreatedEmailHandler : INotificationHandler<OrderCreated>
            {
                public ValueTask HandleAsync(OrderCreated notification, CancellationToken cancellationToken)
                    => default;
            }
            """,
            """
            using Mocha.Mediator;

            namespace TestApp;

            public partial class OrderCreatedEmailHandler : INotificationHandler<OrderCreated>;
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_ReportDuplicateHandler_When_TwoDifferentHandlerTypesImplementSameCommand()
    {
        // The dedup fix in ValidateMessageHandlerPairing collapses a handler restated across
        // partial declaration parts by handler type name. It must not over-dedupe: two DIFFERENT
        // handler types for the same command, one of them partial, still have distinct fully
        // qualified names, so MO0002 must still be reported for both.
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record CreateOrderCommand(string Name) : ICommand<int>;

            public partial class CreateOrderHandlerA : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(1);
            }
            """,
            """
            using Mocha.Mediator;

            namespace TestApp;

            public partial class CreateOrderHandlerA : ICommandHandler<CreateOrderCommand, int>;
            """,
            """
            using Mocha.Mediator;

            namespace TestApp;

            public class CreateOrderHandlerB : ICommandHandler<CreateOrderCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
                    => new(2);
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_PartialQueryHandler_AcrossFiles_MatchesSnapshot()
    {
        // Item 9: Partial query handler split across two syntax trees
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record GetOrderQuery(int Id) : IQuery<string>;

            public partial class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, string>
            {
                public ValueTask<string> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
                    => new(FormatOrder(query.Id));
            }
            """,
            """
            namespace TestApp;

            public partial class GetOrderQueryHandler
            {
                private static string FormatOrder(int id) => $"Order-{id}";
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
