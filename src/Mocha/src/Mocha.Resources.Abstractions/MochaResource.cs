using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// Base class for a single Mocha resource — a typed, JSON-emitting descriptor
/// for a specific kind of artifact in a Mocha-based service (a queue, an exchange,
/// a saga state, a dispatch endpoint, …).
/// </summary>
public abstract class MochaResource
{
    /// <summary>
    /// Gets the kind discriminator of this resource (e.g. <c>"mocha.queue"</c>).
    /// </summary>
    public abstract string Kind { get; }

    /// <summary>
    /// Gets the deterministic, stable identifier of this resource.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Writes this resource's payload as JSON object members directly into the
    /// supplied <paramref name="writer"/>.
    /// </summary>
    /// <remarks>
    /// Implementations must emit object members only — they must not call
    /// <see cref="Utf8JsonWriter.WriteStartObject()"/> or
    /// <see cref="Utf8JsonWriter.WriteEndObject"/> for the outer object, and must not
    /// dispose the writer.
    /// </remarks>
    /// <param name="writer">The writer to emit JSON object members into.</param>
    public abstract void Write(Utf8JsonWriter writer);
}
