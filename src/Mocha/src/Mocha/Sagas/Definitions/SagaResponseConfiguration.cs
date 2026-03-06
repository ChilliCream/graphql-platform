namespace Mocha.Sagas;

/// <summary>
/// Configuration for a response message produced by a saga when it reaches a final state.
/// </summary>
public sealed class SagaResponseConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the CLR type of the response event.
    /// </summary>
    public Type? EventType { get; set; }

    /// <summary>
    /// Gets or sets the factory that creates the response event from the saga state.
    /// </summary>
    public Func<object, object>? Factory { get; set; }
}
