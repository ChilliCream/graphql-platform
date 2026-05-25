namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Default options for exchanges created by topology conventions.
/// </summary>
public sealed class RabbitMQDefaultExchangeOptions
{
    /// <summary>
    /// Gets or sets the default exchange type.
    /// When set, all auto-provisioned exchanges will use this type unless explicitly overridden.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets whether exchanges are durable by default.
    /// Default is null (uses the RabbitMQ default of true).
    /// </summary>
    public bool? Durable { get; set; }

    /// <summary>
    /// Gets or sets whether exchanges are auto-deleted by default.
    /// Default is null (uses the RabbitMQ default of false).
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets additional default arguments applied to all auto-provisioned exchanges.
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();

    /// <summary>
    /// Applies these defaults to an exchange configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(RabbitMQExchangeConfiguration configuration)
    {
        configuration.Type ??= Type;
        configuration.Durable ??= Durable;
        configuration.AutoDelete ??= AutoDelete;

        if (Arguments.Count > 0)
        {
            configuration.Arguments ??= new Dictionary<string, object>();

            foreach (var (key, value) in Arguments)
            {
                configuration.Arguments.TryAdd(key, value);
            }
        }
    }
}
