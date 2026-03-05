using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Configuration for a message that is sent as a side effect of a saga transition or lifecycle action.
/// </summary>
public sealed class SagaEventSendConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the CLR type of the message to send.
    /// </summary>
    public required Type MessageType { get; set; }

    /// <summary>
    /// Gets or sets the factory that creates the message from the consume context and saga state.
    /// </summary>
    public required Func<IConsumeContext, object, object?> Factory { get; set; }

    /// <summary>
    /// Gets or sets the send options for the message.
    /// </summary>
    public required SagaSendOptions Options { get; set; }
}
