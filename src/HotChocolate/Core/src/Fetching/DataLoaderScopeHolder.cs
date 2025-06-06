using System.Collections.Frozen;
using GreenDonut;
using GreenDonut.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

/// <summary>
/// This instance holds the current DataLoader scope and allows to create a new scope.
/// </summary>
public sealed class DataLoaderScopeHolder
{
    private static readonly AsyncLocal<InstanceHolder> s_currentScope = new();
    private readonly IReadOnlyDictionary<Type, DataLoaderRegistration> _registrations;

    public DataLoaderScopeHolder(DataLoaderRegistrar registrar)
        => _registrations = registrar.Registrations;

    /// <summary>
    /// Creates and pins a new <see cref="IDataLoaderScope"/>.
    /// </summary>
    public IDataLoaderScope PinNewScope(IServiceProvider scopedServiceProvider, IBatchScheduler? scheduler = null)
    {
        scheduler ??= scopedServiceProvider.GetRequiredService<IBatchScheduler>();
        return CurrentScope = new ExecutionDataLoaderScope(scopedServiceProvider, scheduler, _registrations);
    }

    public IDataLoaderScope GetOrCreateScope(IServiceProvider scopedServiceProvider, IBatchScheduler? scheduler = null)
    {
        if(s_currentScope.Value?.Scope is null)
        {
            CurrentScope = PinNewScope(scopedServiceProvider, scheduler);
        }
        return CurrentScope;
    }

    /// <summary>
    /// Gets access to the current <see cref="IDataLoaderScope"/> instance.
    /// </summary>
    /// <exception cref="InvalidCastException">
    /// The instance was not initialized.
    /// </exception>
    public IDataLoaderScope CurrentScope
    {
        get => s_currentScope.Value?.Scope ??
            throw new InvalidOperationException("No DataLoader scope exists.");
        set
        {
            var holder = s_currentScope.Value;

            if (holder is null)
            {
                holder = new InstanceHolder();
                s_currentScope.Value = holder;
            }

            holder.Scope = value;
        }
    }

    private sealed class InstanceHolder
    {
        public IDataLoaderScope Scope = null!;
    }
}
