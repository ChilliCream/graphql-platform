using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Mediator;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Mediator middleware that wraps command handling in a database transaction.
/// Commits on success and rolls back on failure.
/// Queries and notifications are excluded by default but can be opted in via
/// <see cref="MediatorEntityFrameworkOptions.ShouldCreateTransaction"/>.
/// </summary>
internal sealed class EntityFrameworkTransactionMiddleware(
    Type dbContextType,
    Func<IMediatorContext, bool>? shouldCreateTransaction)
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        if (shouldCreateTransaction is not null && !shouldCreateTransaction(context))
        {
            await next(context);
            return;
        }

        var dbContext = (DbContext)context.Services.GetRequiredService(dbContextType);
        var strategy = dbContext.Database.CreateExecutionStrategy();

        if (strategy.RetriesOnFailure)
        {
            await strategy.ExecuteAsync(async ct =>
            {
                await using var transaction =
                    await dbContext.Database.BeginTransactionAsync(ct);

                try
                {
                    await next(context);

                    await dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);

                    throw;
                }
            }, context.CancellationToken);
        }
        else
        {
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

            try
            {
                await next(context);

                await dbContext.SaveChangesAsync(context.CancellationToken);
                await transaction.CommitAsync(context.CancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(context.CancellationToken);

                throw;
            }
        }
    }

    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var feature = factoryCtx.Features.GetRequired<EntityFrameworkTransactionFeature>();

                // When no custom predicate is set, apply the default policy at compile
                // time: commands get transactions, queries and notifications do not.
                // This eliminates the middleware entirely from those pipelines (zero cost).
                if (feature.ShouldCreateTransaction is null
                    && (factoryCtx.IsQuery() || factoryCtx.IsNotification()))
                {
                    return next;
                }

                var middleware = new EntityFrameworkTransactionMiddleware(
                    feature.ContextType,
                    feature.ShouldCreateTransaction);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "EntityFrameworkTransaction");
}
