namespace Mocha.Sagas;

/// <summary>
/// Configuration for a single state within a saga state machine.
/// </summary>
public sealed class SagaStateConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the name of the state.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the initial state of the saga.
    /// </summary>
    public bool IsInitial { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a final (completed) state of the saga.
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    /// Gets or sets the list of transitions that can occur from this state.
    /// </summary>
    public List<SagaTransitionConfiguration> Transitions { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional response configuration for final states.
    /// </summary>
    public SagaResponseConfiguration? Response { get; set; }

    /// <summary>
    /// Gets or sets the lifecycle configuration that executes when the saga enters this state.
    /// </summary>
    public SagaLifeCycleConfiguration OnEntry { get; set; } = new();
}
