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
        ]).MatchMarkdownAsync();
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
        ]).MatchMarkdownAsync();
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
        ]).MatchMarkdownAsync();
    }
}
