namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The type configuration container is a helper that allows us to store
/// type configurations that are applied to a type during the type initialization.
/// </summary>
public sealed class TypeConfigurationContainer
{
    private readonly object _sync = new();
    private readonly HashSet<string> _created = [];
    private readonly Dictionary<Type, List<object>> _configurations = [];
    private readonly Dictionary<string, List<object>> _namedConfigurations = [];

    public void TryAdd<TRuntimeType>(
        string key,
        Func<Action<IObjectTypeDescriptor<TRuntimeType>>> configureFactory)
        => TryAddInternal(key, typeof(TRuntimeType), configureFactory);

    public void TryAdd(
        string key,
        string typeName,
        Func<Action<IObjectTypeDescriptor>> configureFactory)
        => TryAddInternal(key, typeName, configureFactory);

    public void TryAdd<TRuntimeType>(
        string key,
        Func<Action<IInterfaceTypeDescriptor<TRuntimeType>>> configureFactory)
        => TryAddInternal(key, typeof(TRuntimeType), configureFactory);

    private void TryAddInternal(string key, Type runtimeType, Func<object> configureFactory)
    {
        lock (_sync)
        {
            if (!_created.Add(key))
            {
                return;
            }

            if (!_configurations.TryGetValue(runtimeType, out var list))
            {
                list = [];
                _configurations[runtimeType] = list;
            }

            list.Add(configureFactory());
        }
    }

    private void TryAddInternal(string key, string typeName, Func<object> configureFactory)
    {
        lock (_sync)
        {
            if (!_created.Add(key))
            {
                return;
            }

            if (!_namedConfigurations.TryGetValue(typeName, out var list))
            {
                list = [];
                _namedConfigurations[typeName] = list;
            }

            list.Add(configureFactory());
        }
    }

    internal void Apply<TDescriptor>(
        Type runtimeType,
        TDescriptor descriptor)
        where TDescriptor : IDescriptor
    {
        lock (_sync)
        {
            if (_configurations.TryGetValue(runtimeType, out var list))
            {
                foreach (var item in list)
                {
                    if (item is Action<TDescriptor> configure)
                    {
                        configure(descriptor);
                    }
                }
            }
        }
    }

    internal void Apply<TDescriptor>(
        string typeName,
        TDescriptor descriptor)
        where TDescriptor : IDescriptor
    {
        lock (_sync)
        {
            if (_namedConfigurations.TryGetValue(typeName, out var list))
            {
                foreach (var item in list)
                {
                    if (item is Action<TDescriptor> configure)
                    {
                        configure(descriptor);
                    }
                }
            }
        }
    }
}
