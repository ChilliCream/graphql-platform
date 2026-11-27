using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal sealed class EntityFrameworkConsumerExecutionStrategy(Type contextType) : IConsumerExecutionStrategy
{
    public async ValueTask ExecuteAsync(IConsumeContext context, Func<CancellationToken, ValueTask> executeAttempt)
    {
        await using var scope = context.Services.CreateAsyncScope();
        var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
        var strategy = dbContext.Database.CreateExecutionStrategy();

        if (!strategy.RetriesOnFailure)
        {
            await executeAttempt(context.CancellationToken);
            return;
        }

        try
        {
            await strategy.ExecuteAsync(
                executeAttemptToken => executeAttempt(executeAttemptToken).AsTask(),
                context.CancellationToken);
        }
        catch (RetryLimitExceededException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
