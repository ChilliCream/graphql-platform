using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal sealed class DbContextAccessor(DbContext context, bool ownsContext) : IDisposable, IAsyncDisposable
{
    private bool _ownsContext = ownsContext;

    public DbContext Context { get; } = context;

    public static DbContextAccessor Create<TContext>(IServiceProvider services) where TContext : DbContext
    {
        if (services.GetService<TContext>() is { } context)
        {
            return new DbContextAccessor(context, false);
        }

        if (services.GetService<IDbContextFactory<TContext>>() is { } factory)
        {
            var createdContext = factory.CreateDbContext();

            try
            {
                if (services.GetServices<OutboxDbContextRegistration>().Any(r => r.ContextType == typeof(TContext)))
                {
                    createdContext.GetService<DbContextOutboxScope>().Attach(services);
                }

                return new DbContextAccessor(createdContext, true);
            }
            catch
            {
                createdContext.Dispose();
                throw;
            }
        }

        throw ThrowHelper.MissingMessagingDbContext(typeof(TContext));
    }

    public void Dispose()
    {
        if (_ownsContext)
        {
            _ownsContext = false;
            Context.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_ownsContext)
        {
            _ownsContext = false;
            return Context.DisposeAsync();
        }

        return default;
    }
}
