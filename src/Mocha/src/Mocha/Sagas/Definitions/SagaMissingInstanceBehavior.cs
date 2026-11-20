namespace Mocha.Sagas;

/// <summary>
/// Specifies how a saga handles an event correlated through <see cref="ICorrelatable"/> for which no saga
/// instance exists.
/// </summary>
public enum SagaMissingInstanceBehavior
{
    /// <summary>
    /// The event faults with a <see cref="SagaInstanceNotFoundException"/>, which exception policies can
    /// retry, redeliver, or dead-letter.
    /// </summary>
    Fault,

    /// <summary>
    /// The event is acknowledged without being applied.
    /// </summary>
    Ignore
}
