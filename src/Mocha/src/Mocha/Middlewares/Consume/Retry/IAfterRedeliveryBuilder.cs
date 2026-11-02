namespace Mocha;

/// <summary>
/// Builder for chaining actions after redelivery configuration.
/// If nothing is chained, the default behavior (dead-letter on exhaustion) applies.
/// </summary>
public interface IAfterRedeliveryBuilder
{
    /// <summary>
    /// Routes the message to the error endpoint after redelivery exhaustion.
    /// </summary>
    void ThenDeadLetter();
}
