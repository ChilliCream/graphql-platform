namespace Mocha;

/// <summary>
/// Stores descriptor configuration actions that are applied when messaging types are initialized.
/// </summary>
public sealed class MessagingConfigurationContainer
{
    private readonly HashSet<(Type DescriptorType, string Key)> _created = [];
    private readonly Dictionary<(Type RuntimeType, Type DescriptorType), List<object>> _configurations = [];

    public void Add<TDescriptor>(
        Type runtimeType,
        Action<TDescriptor> configure)
        where TDescriptor : IMessagingDescriptor
    {
        ArgumentNullException.ThrowIfNull(runtimeType);
        ArgumentNullException.ThrowIfNull(configure);

        AddInternal(runtimeType, typeof(TDescriptor), configure);
    }

    public void TryAdd<TDescriptor>(
        string key,
        Type runtimeType,
        Action<TDescriptor> configure)
        where TDescriptor : IMessagingDescriptor
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(runtimeType);
        ArgumentNullException.ThrowIfNull(configure);

        TryAddInternal(key, typeof(TDescriptor), runtimeType, configure);
    }

    internal void Apply<TDescriptor>(
        Type runtimeType,
        TDescriptor descriptor)
        where TDescriptor : IMessagingDescriptor
    {
        ArgumentNullException.ThrowIfNull(runtimeType);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_configurations.TryGetValue((runtimeType, typeof(TDescriptor)), out var configurations))
        {
            return;
        }

        foreach (var item in configurations)
        {
            ((Action<TDescriptor>)item)(descriptor);
        }
    }

    private void AddInternal(Type runtimeType, Type descriptorType, object configuration)
    {
        if (!_configurations.TryGetValue((runtimeType, descriptorType), out var list))
        {
            list = [];
            _configurations[(runtimeType, descriptorType)] = list;
        }

        list.Add(configuration);
    }

    private void TryAddInternal(string key, Type descriptorType, Type runtimeType, object configuration)
    {
        if (!_created.Add((descriptorType, key)))
        {
            return;
        }

        if (!_configurations.TryGetValue((runtimeType, descriptorType), out var list))
        {
            list = [];
            _configurations[(runtimeType, descriptorType)] = list;
        }

        list.Add(configuration);
    }
}
