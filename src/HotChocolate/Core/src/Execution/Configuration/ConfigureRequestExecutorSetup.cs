namespace HotChocolate.Execution.Configuration;

public sealed class ConfigureRequestExecutorSetup : IConfigureRequestExecutorSetup
{
    private readonly Action<RequestExecutorSetup> _configure;

    public ConfigureRequestExecutorSetup(
        string schemaName,
        Action<RequestExecutorSetup> configure)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new ArgumentNullException(nameof(schemaName));
        }

        SchemaName = schemaName;
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    public ConfigureRequestExecutorSetup(
        string schemaName,
        RequestExecutorSetup options)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new ArgumentNullException(nameof(schemaName));
        }

        SchemaName = schemaName;
        _configure = options.CopyTo;
    }

    public string SchemaName { get; }

    public void Configure(RequestExecutorSetup options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _configure(options);
    }
}
