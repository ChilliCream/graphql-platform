namespace Mocha.Analyzers.Tests;

/// <summary>
/// Tests that <c>KnownTypeSymbols</c> correctly resolves (or fails to resolve) Mocha mediator
/// type symbols from a compilation. These tests exercise symbol resolution indirectly through
/// the source generator via <see cref="TestHelper.GetGeneratedSourceSnapshot"/>.
/// </summary>
public class KnownTypeSymbolsTests
{
    [Fact]
    public async Task Generate_WithAllHandlerTypes_AllSymbolsResolved_MatchesSnapshot()
    {
        // This exercises resolution of KnownTypeSymbols properties by including
        // every handler type. If any symbol fails to resolve, the generator would not
        // produce the expected registrations.
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            // ICommand (void) -> ICommandHandlerVoid
            public record VoidCommand() : ICommand;
            public class VoidCommandHandler : ICommandHandler<VoidCommand>
            {
                public ValueTask HandleAsync(VoidCommand cmd, CancellationToken ct) => default;
            }

            // ICommand<T> -> ICommandHandlerResponse
            public record ResponseCommand() : ICommand<int>;
            public class ResponseCommandHandler : ICommandHandler<ResponseCommand, int>
            {
                public ValueTask<int> HandleAsync(ResponseCommand cmd, CancellationToken ct) => new(42);
            }

            // IQuery<T> -> IQueryHandler
            public record MyQuery() : IQuery<string>;
            public class MyQueryHandler : IQueryHandler<MyQuery, string>
            {
                public ValueTask<string> HandleAsync(MyQuery q, CancellationToken ct) => new("result");
            }

            // INotification -> INotificationHandler
            public record MyEvent() : INotification;
            public class MyEventHandler : INotificationHandler<MyEvent>
            {
                public ValueTask HandleAsync(MyEvent n, CancellationToken ct) => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_WithoutMochaUsings_NoHandlersRegistered_MatchesSnapshot()
    {
        // When code does not reference Mocha types at all, no handler registrations
        // should be generated (KnownTypeSymbols properties return null and the generator
        // skips the compilation).
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            namespace TestApp;

            public class PlainService
            {
                public void DoWork() { }
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_CommandVoidResolution_ICommandInterface_MatchesSnapshot()
    {
        // Tests that the ICommandVoid symbol resolution correctly identifies ICommand
        // (the marker interface without TResponse).
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record FireAndForgetCommand(string Data) : ICommand;

            public class FireAndForgetHandler : ICommandHandler<FireAndForgetCommand>
            {
                public ValueTask HandleAsync(FireAndForgetCommand cmd, CancellationToken ct) => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_CommandOfTResolution_ICommandGeneric_MatchesSnapshot()
    {
        // Tests that the ICommandOfT symbol resolution correctly identifies ICommand<TResponse>.
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record ComputeCommand(int X, int Y) : ICommand<long>;

            public class ComputeHandler : ICommandHandler<ComputeCommand, long>
            {
                public ValueTask<long> HandleAsync(ComputeCommand cmd, CancellationToken ct)
                    => new((long)cmd.X + cmd.Y);
            }
            """
        ]).MatchMarkdownAsync();
    }
}
