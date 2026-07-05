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
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
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
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
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
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitTrackingMetadata_When_MediatorHandlerHasXmlDocumentation()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            /// <summary>
            /// Deletes an order from the system.
            /// </summary>
            public record DeleteOrderCommand(int OrderId) : ICommand;

            /// <summary>
            /// Handles delete order commands.
            /// </summary>
            public class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_OmitSourceMetadata_When_EmitSourceMetadataDisabled()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            /// <summary>
            /// Deletes an order from the system.
            /// </summary>
            public record DeleteOrderCommand(int OrderId) : ICommand;

            /// <summary>
            /// Handles delete order commands.
            /// </summary>
            public class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ],
        emitSourceMetadata: false).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitProjectRelativePath_When_ProjectDirProvided()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            /// <summary>
            /// Deletes an order from the system.
            /// </summary>
            public record DeleteOrderCommand(int OrderId) : ICommand;

            /// <summary>
            /// Handles delete order commands.
            /// </summary>
            public class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ],
        projectDir: "/app/",
        sourcePaths: ["/app/src/Handlers/DeleteOrderHandler.cs"]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Should_EmitRepositoryUrlAndCommit_When_ProvidedByBuild()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            /// <summary>
            /// Deletes an order from the system.
            /// </summary>
            public record DeleteOrderCommand(int OrderId) : ICommand;

            /// <summary>
            /// Handles delete order commands.
            /// </summary>
            public class DeleteOrderHandler : ICommandHandler<DeleteOrderCommand>
            {
                public ValueTask HandleAsync(DeleteOrderCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ],
        repositoryUrl: "https://github.com/example/repo",
        commit: "abc123def456").MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
