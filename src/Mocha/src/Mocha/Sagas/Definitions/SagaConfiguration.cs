namespace Mocha.Sagas;

/// <summary>
/// Configuration for a saga state machine, including its name, states, and serializer.
/// </summary>
public sealed class SagaConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the name of the saga.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the list of state configurations that define the saga's state machine.
    /// </summary>
    public List<SagaStateConfiguration> States { get; set; } = [];

    /// <summary>
    /// Gets or sets the state configuration for transitions that apply to all non-initial and non-final states.
    /// </summary>
    public SagaStateConfiguration? DuringAny { get; set; }

    /// <summary>
    /// Gets or sets a factory for creating a custom saga state serializer.
    /// </summary>
    public Func<IServiceProvider, ISagaStateSerializer>? StateSerializer { get; set; }
}
