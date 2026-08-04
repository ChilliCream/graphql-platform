namespace Mocha.Mediator.Benchmarks.Messaging;

// Command for Wolverine (plain record, no interface needed)
public sealed record WolverineCommand(Guid Id);

// Notification for Wolverine
public sealed record WolverineNotification(Guid Id);

// Command handler for Wolverine (convention-based)
#pragma warning disable RCS1102 // Wolverine convention requires instance handlers for DI registration
public class WolverineCommandHandler
{
    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());

    public BenchmarkResponse Handle(WolverineCommand command) => _response;
}

// Notification handler for Wolverine (convention-based)
public class WolverineNotificationHandler
{
    public void Handle(WolverineNotification notification) { }
}
#pragma warning restore RCS1102
