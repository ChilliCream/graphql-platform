namespace Mocha.Middlewares;

/// <summary>
/// Receives a prepared envelope instead of sending it to a transport.
/// </summary>
internal interface IMessageDispatchTarget
{
    /// <summary>
    /// Captures an independent copy of the envelope before its dispatch context is released.
    /// </summary>
    void Capture(MessageEnvelope envelope);
}
