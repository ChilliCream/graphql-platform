using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class SourceSchemaClientConfigurations
{
    private readonly FrozenDictionary<string, Configuration> _configurations;

    public SourceSchemaClientConfigurations(IEnumerable<ISourceSchemaClientConfiguration> configurations)
    {
        ArgumentNullException.ThrowIfNull(configurations);

        var dictionary = new Dictionary<string, Configuration>();

        foreach (var configuration in configurations)
        {
            Set(dictionary, configuration);
        }

        _configurations = dictionary.ToFrozenDictionary();
    }

    public bool TryGet(
        string name,
        OperationType operationType,
        [NotNullWhen(true)] out ISourceSchemaClientConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!_configurations.TryGetValue(name, out var config))
        {
            configuration = null;
            return false;
        }

        configuration = operationType switch
        {
            OperationType.Query => config.Query,
            OperationType.Mutation => config.Mutation,
            OperationType.Subscription => config.Subscription,
            _ => null
        };

        return configuration is not null;
    }

    private static void Set(
        Dictionary<string, Configuration> configurations,
        ISourceSchemaClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configurations.TryGetValue(configuration.Name, out var config))
        {
            config = new Configuration(configuration.Name);
            configurations.Add(configuration.Name, config);
        }

        if ((configuration.SupportedOperations & SupportedOperationType.Query) == SupportedOperationType.Query)
        {
            config.Query = configuration;
        }

        if ((configuration.SupportedOperations & SupportedOperationType.Mutation) == SupportedOperationType.Mutation)
        {
            config.Mutation = configuration;
        }

        if ((configuration.SupportedOperations & SupportedOperationType.Subscription) == SupportedOperationType.Subscription)
        {
            config.Subscription = configuration;
        }
    }

    private sealed class Configuration(string name)
    {
        public string Name = name;

        public ISourceSchemaClientConfiguration? Query;

        public ISourceSchemaClientConfiguration? Mutation;

        public ISourceSchemaClientConfiguration? Subscription;
    }
}
