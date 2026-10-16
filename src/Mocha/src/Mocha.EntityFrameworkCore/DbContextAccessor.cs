using Microsoft.EntityFrameworkCore;
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
            return new DbContextAccessor(factory.CreateDbContext(), true);
        }

        throw new InvalidOperationException(
            $"The messaging DbContext '{typeof(TContext).FullName}' is not registered. "
            + "Register the context or an IDbContextFactory for it before building the service provider.");
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
