using System.Text.Json.Serialization;

namespace Mocha.Sagas;

/// <summary>
/// Base class for saga state objects, providing the saga instance identifier, current state name,
/// error history, and custom metadata.
/// </summary>
/// <param name="id">The unique identifier of the saga instance.</param>
/// <param name="state">The current state name.</param>
public class SagaStateBase(Guid id, string state)
{
    /// <summary>
    /// Initializes a new saga state with an auto-generated identifier and the initial state.
    /// </summary>
    public SagaStateBase() : this(Guid.NewGuid(), StateNames.Initial) { }

    /// <summary>
    /// Gets or sets the unique identifier of the saga instance.
    /// </summary>
    public Guid Id { get; set; } = id;

    /// <summary>
    /// Gets or sets the current state name of the saga instance.
    /// </summary>
    public string State { get; set; } = state;

    /// <summary>
    /// Gets or sets the list of errors that have occurred during saga execution.
    /// </summary>
    public List<SagaError> Errors { get; set; } = [];

    /// <summary>
    /// Gets or sets the cancellation token for a scheduled timeout, used to cancel the timeout
    /// when the saga completes or transitions before the timeout fires.
    /// </summary>
    public string? TimeoutToken { get; set; }

    /// <summary>
    /// Gets or sets custom metadata associated with this saga instance.
    /// </summary>
    [JsonConverter(typeof(HeadersJsonConverter))]
    public IHeaders Metadata { get; set; } = new Headers();
}
