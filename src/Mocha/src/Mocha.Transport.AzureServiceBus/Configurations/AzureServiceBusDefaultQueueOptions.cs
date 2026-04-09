namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Default options for queues created by topology conventions.
/// </summary>
public sealed class AzureServiceBusDefaultQueueOptions
{
    /// <summary>
    /// Gets or sets whether queues are auto-deleted by default.
    /// Default is null (uses the queue default of false).
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets whether queues are auto-provisioned by default.
    /// Default is null (uses the queue default of true).
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Applies these defaults to a queue configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(AzureServiceBusQueueConfiguration configuration)
    {
        configuration.AutoProvision ??= AutoProvision;
        configuration.AutoDelete ??= AutoDelete;
    }
}
