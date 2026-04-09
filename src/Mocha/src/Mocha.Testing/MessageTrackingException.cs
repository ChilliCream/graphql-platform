namespace Mocha.Testing;

/// <summary>
/// An exception thrown when a message tracking assertion fails,
/// containing diagnostic output to help identify the root cause.
/// </summary>
public sealed class MessageTrackingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTrackingException"/> class.
    /// </summary>
    /// <param name="message">The assertion failure message.</param>
    /// <param name="diagnosticOutput">The diagnostic output from the tracker.</param>
    public MessageTrackingException(string message, string diagnosticOutput)
        : base($"{message}\n\n{diagnosticOutput}")
    {
        DiagnosticOutput = diagnosticOutput;
    }

    /// <summary>
    /// Gets the diagnostic output from the message tracker at the time of failure.
    /// </summary>
    public string DiagnosticOutput { get; }
}
