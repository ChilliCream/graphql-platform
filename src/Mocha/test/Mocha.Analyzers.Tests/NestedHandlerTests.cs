namespace Mocha.Analyzers.Tests;

public class NestedHandlerTests
{
    [Fact]
    public async Task Generate_NestedClassHandler_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record DeleteOrderCommand(int OrderId) : ICommand;

            public class Outer
            {
                public class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
                {
                    public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                        => default;
                }
            }
            """
        ]).MatchMarkdownAsync();
    }
}
