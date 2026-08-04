using System.Text.Json;

namespace Mocha.Sagas.EfCore;

/// <summary>
/// Represents the persisted state of a saga instance stored via Entity Framework Core.
/// </summary>
/// <param name="id">The unique identifier of the saga instance.</param>
/// <param name="sagaName">The logical name identifying the saga type.</param>
/// <param name="state">The serialized saga state as a JSON document.</param>
/// <param name="createdAt">The timestamp when this saga state was first persisted.</param>
/// <param name="updatedAt">The timestamp of the most recent state update.</param>
public sealed class SagaState(
    Guid id,
    string sagaName,
    JsonDocument state,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt)
{
    /// <summary>
    /// Gets or sets the unique identifier of the saga instance.
    /// </summary>
    public Guid Id { get; set; } = id;

    /// <summary>
    /// Gets or sets the logical name identifying the saga type.
    /// </summary>
    public string SagaName { get; set; } = sagaName;

    /// <summary>
    /// Gets or sets the serialized saga state as a JSON document.
    /// </summary>
    public JsonDocument State { get; set; } = state;

    /// <summary>
    /// Gets or sets the timestamp when this saga state was first persisted.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = createdAt;

    /// <summary>
    /// Gets or sets the timestamp of the most recent state update.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = updatedAt;

    /// <summary>
    /// Gets or sets the concurrency token used for optimistic concurrency control during updates.
    /// </summary>
    public Guid Version { get; set; }
}
