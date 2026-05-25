namespace Mocha.Analyzers.Tests;

public class GenericHandlerTests
{
    [Fact]
    public async Task Generate_GenericBaseHandler_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public abstract class BaseHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
                where TCommand : ICommand<TResponse>
            {
                public abstract ValueTask<TResponse> HandleAsync(TCommand command, CancellationToken ct);
            }

            public record MyCommand(int Id) : ICommand<string>;

            public class MyHandler : BaseHandler<MyCommand, string>
            {
                public override ValueTask<string> HandleAsync(MyCommand command, CancellationToken ct)
                    => new("ok");
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_OpenGenericCommand_MatchesSnapshot()
    {
        // An open generic command type cannot be dispatched by the mediator at
        // runtime, so the generator should either skip it or handle it gracefully.
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record ProcessCommand<T>(T Data) : ICommand<string>;

            public class StringProcessor : ICommandHandler<ProcessCommand<string>, string>
            {
                public ValueTask<string> HandleAsync(ProcessCommand<string> command, CancellationToken ct)
                    => new("processed");
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_MultipleHandlersSameNamespace_DeterministicOrder_MatchesSnapshot()
    {
        // Multiple handlers in the same namespace should produce deterministic output ordering.
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record ZetaCommand() : ICommand;
            public record AlphaCommand() : ICommand;
            public record MidCommand() : ICommand;

            public class ZetaHandler : ICommandHandler<ZetaCommand>
            {
                public ValueTask HandleAsync(ZetaCommand command, CancellationToken ct) => default;
            }

            public class AlphaHandler : ICommandHandler<AlphaCommand>
            {
                public ValueTask HandleAsync(AlphaCommand command, CancellationToken ct) => default;
            }

            public class MidHandler : ICommandHandler<MidCommand>
            {
                public ValueTask HandleAsync(MidCommand command, CancellationToken ct) => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
