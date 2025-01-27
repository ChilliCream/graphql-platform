using System.Collections.Concurrent;
using System.ComponentModel;
using HotChocolate.Types;

namespace HotChocolate.Configuration;

public static class TypeConfiguration
{
    private static readonly object _sync = new();
    private static readonly HashSet<string> _created = [];
    private static readonly Dictionary<Type, List<object>> _configurations = [];

    public static void TryAdd<TRuntimeType>(
        string key,
        Func<Action<IObjectTypeDescriptor<TRuntimeType>>> configureFactory)
        => TryAdd(key, typeof(TRuntimeType), configureFactory);

    public static void TryAdd<TRuntimeType>(
        string key,
        Func<Action<IInterfaceTypeDescriptor<TRuntimeType>>> configureFactory)
        => TryAdd(key, typeof(TRuntimeType), configureFactory);

    private static void TryAdd(string key, Type runtimeType, Func<object> configureFactory)
    {
        lock (_sync)
        {
            if (!_created.Add(key))
            {
                return;
            }

            if (!_configurations.TryGetValue(runtimeType, out var list))
            {
                list = new List<object>();
                _configurations[runtimeType] = list;
            }

            list.Add(configureFactory());
        }
    }

    internal static void Apply<TDescriptor>(
        Type runtimeType,
        TDescriptor descriptor)
        where TDescriptor : IDescriptor
    {
        if (_configurations.TryGetValue(runtimeType, out var list))
        {
            foreach (var item in list)
            {
                if (item is Func<Action<TDescriptor>> factory)
                {
                    factory()(descriptor);
                }
            }
        }
    }
}
