namespace Mocha.Analyzers.Tests;

public class InternalHandlerTests
{
    [Fact]
    public async Task Generate_InternalHandler_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            [assembly: MediatorModule("Test")]

            namespace TestApp;

            public record DeleteOrderCommand(int OrderId) : ICommand;

            internal class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
