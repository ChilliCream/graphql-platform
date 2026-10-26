using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal sealed class EntityFrameworkTransactionConsumeMiddleware(Type contextType)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        var dbContext = (DbContext)context.Services.GetRequiredService(contextType);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            await next(context);

            await transaction.CommitAsync(context.CancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }

    public static ConsumerMiddlewareConfiguration Create(Type contextType)
        => new(
            (_, next) =>
            {
                var middleware = new EntityFrameworkTransactionConsumeMiddleware(contextType);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "EntityFrameworkTransaction");
}
