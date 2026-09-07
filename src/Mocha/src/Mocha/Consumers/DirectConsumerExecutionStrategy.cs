namespace Mocha;

internal sealed class DirectConsumerExecutionStrategy : IConsumerExecutionStrategy
{
    public static DirectConsumerExecutionStrategy Instance { get; } = new();

    public ValueTask ExecuteAsync(IConsumeContext context, Func<CancellationToken, ValueTask> executeAttempt)
        => executeAttempt(context.CancellationToken);
}
