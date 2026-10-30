namespace Mocha.Sagas;

/// <summary>
/// Extension methods for <see cref="ISagaDescriptor{TState}"/>.
/// </summary>
public static class SagaDescriptorExtensions
{
    /// <summary>
    /// Configures a timeout for the saga, creating a timed-out final state.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <param name="descriptor">The saga descriptor to configure.</param>
    /// <param name="timeout">The duration after which the saga times out.</param>
    /// <returns>A descriptor for configuring the timed-out final state.</returns>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public static ISagaFinalStateDescriptor<TState> Timeout<TState>(
        this ISagaDescriptor<TState> descriptor,
        TimeSpan timeout)
        where TState : SagaStateBase
    {
        descriptor.Extend().Configuration.Timeout = timeout;

        return descriptor.Finally(StateNames.TimedOut);
    }

    /// <summary>
    /// Configures how the saga handles an event correlated through <see cref="ICorrelatable"/> for which no
    /// saga instance exists.
    /// The default is <see cref="SagaMissingInstanceBehavior.Fault"/>.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <param name="descriptor">The saga descriptor to configure.</param>
    /// <param name="behavior">The behavior for such an event.</param>
    /// <returns>This descriptor for chaining.</returns>
    public static ISagaDescriptor<TState> OnMissingInstance<TState>(
        this ISagaDescriptor<TState> descriptor,
        SagaMissingInstanceBehavior behavior)
        where TState : SagaStateBase
    {
        descriptor.Extend().Configuration.MissingInstanceBehavior = behavior;

        return descriptor;
    }
}
