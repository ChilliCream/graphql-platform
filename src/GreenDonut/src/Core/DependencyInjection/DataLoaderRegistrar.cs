#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace GreenDonut.DependencyInjection;

/// <summary>
/// Provides access to the DataLoader registrations.
/// </summary>
public sealed class DataLoaderRegistrar
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataLoaderRegistrar"/> class.
    /// </summary>
    /// <param name="registrations">
    /// The DataLoader registrations.
    /// </param>
    public DataLoaderRegistrar(IEnumerable<DataLoaderRegistration> registrations)
    {
        var map = new Dictionary<Type, DataLoaderRegistration>();

        foreach (var registration in registrations)
        {
            map[registration.ServiceType] = registration;
        }

        foreach (var registration in map.Values.ToList())
        {
            if (registration.ServiceType != registration.InstanceType &&
                !map.ContainsKey(registration.InstanceType))
            {
                map[registration.InstanceType] = registration;
            }
        }
#if NET8_0_OR_GREATER
        Registrations = map.ToFrozenDictionary();
#else
        Registrations = map;
#endif
    }

    /// <summary>
    /// Gets the DataLoader registrations.
    /// </summary>
    public IReadOnlyDictionary<Type, DataLoaderRegistration> Registrations { get; }
}
