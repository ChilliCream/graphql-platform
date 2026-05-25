namespace Mocha.Analyzers.Tests;

public class ExplicitModuleNameTests
{
    [Fact]
    public async Task Generate_ModuleWithOnlyName_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            [assembly: MediatorModule("Test2")]

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
}
