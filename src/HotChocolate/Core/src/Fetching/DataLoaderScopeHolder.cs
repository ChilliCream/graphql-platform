using System;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HotChocolate.Fetching;

/// <summary>
/// This instance holds the current DataLoader scope and allows to create a new scope.
/// </summary>
public sealed class DataLoaderScopeHolder
{
    private static readonly AsyncLocal<InstanceHolder> _currentScope = new();
#if NET8_0_OR_GREATER
    private readonly FrozenDictionary<Type, DataLoaderRegistration> _registrations;
#else
    private readonly Dictionary<Type, DataLoaderRegistration> _registrations;    
#endif

    public DataLoaderScopeHolder(IEnumerable<DataLoaderRegistration> registrations)
    {
#if NET8_0_OR_GREATER
        _registrations = CreateRegistrations().ToFrozenDictionary(t => t.Item1, t => t.Item2);
#else
        _registrations = CreateRegistrations().ToDictionary(t => t.Item1, t => t.Item2);
#endif
        
        IEnumerable<(Type, DataLoaderRegistration)> CreateRegistrations()
        {
            foreach (var reg in registrations)
            {
                if (reg.ServiceType == reg.InstanceType)
                {
                    yield return (reg.ServiceType, reg);
                }
                else
                {
                    yield return (reg.ServiceType, reg);
                    yield return (reg.InstanceType, reg);
                }
            }
        }
    }

    /// <summary>
    /// Creates and pins a new <see cref="IDataLoaderScope"/>.
    /// </summary>
    /// <returns></returns>
    public IDataLoaderScope PinNewScope(IServiceProvider scopedServiceProvider)
        => CurrentScope = new DefaultDataLoaderScope(scopedServiceProvider, _registrations);

    /// <summary>
    /// Gets access to the current <see cref="IDataLoaderScope"/> instance.
    /// </summary>
    /// <exception cref="InvalidCastException">
    /// The instance was not initialized.
    /// </exception>
    public IDataLoaderScope CurrentScope
    {
        get => _currentScope.Value?.Scope ??
            throw new InvalidCastException(
                "Can only be accessed in an async context.");
        set
        {
            var holder = _currentScope.Value;

            if (holder is null)
            {
                holder = new InstanceHolder();
                _currentScope.Value = holder;
            }

            holder.Scope = value;
        }
    }

    private sealed class InstanceHolder
    {
        public IDataLoaderScope Scope = default!;
    }
}