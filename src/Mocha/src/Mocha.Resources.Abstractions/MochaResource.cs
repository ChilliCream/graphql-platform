using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// Base class for a single Mocha resource — a typed, JSON-emitting descriptor
/// for a specific kind of artifact in a Mocha-based service (a queue, an exchange,
/// a saga state, a dispatch endpoint, …).
/// </summary>
/// <remarks>
/// <para>
/// Concrete subclasses are typically <c>internal</c> to the contributing library;
/// only the abstract <see cref="MochaResource"/> base, <see cref="MochaResourceDefinition"/>,
/// <see cref="MochaResourceSource"/>, and the composite types are intended to be public.
/// Each subclass holds its own typed fields and emits them via <see cref="Write(Utf8JsonWriter)"/>.
/// </para>
/// <para>
/// Cross-resource references are expressed as id-string members in <see cref="Write(Utf8JsonWriter)"/>
/// (e.g. <c>"queue_id": "..."</c>); there is no separate links collection on the framework type.
/// </para>
/// </remarks>
public abstract class MochaResource
{
    /// <summary>
    /// Gets the kind discriminator of this resource (e.g. <c>"mocha.queue"</c>).
    /// </summary>
    /// <remarks>
    /// Concrete property so consumers can filter or group without parsing JSON.
    /// </remarks>
    public abstract string Kind { get; }

    /// <summary>
    /// Gets the deterministic, stable identifier of this resource.
    /// </summary>
    /// <remarks>
    /// Identifiers are URN-shaped (e.g. <c>urn:mocha:rabbitmq:queue:&lt;transportId&gt;:&lt;queueName&gt;</c>)
    /// and are computed by the contributor. Use <see cref="MochaUrn"/> to keep the scheme consistent.
    /// </remarks>
    public abstract string Id { get; }

    /// <summary>
    /// Writes this resource's payload as JSON object members directly into the
    /// supplied <paramref name="writer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations must emit object members only — they must NOT call
    /// <see cref="Utf8JsonWriter.WriteStartObject()"/> or
    /// <see cref="Utf8JsonWriter.WriteEndObject"/> for the outer object, and must NOT
    /// dispose the writer. The wrapping <c>{ ... }</c> is opened and closed by the
    /// consumer's serializer, which calls this method between
    /// <c>WriteStartObject("attributes")</c> and <c>WriteEndObject()</c>.
    /// </para>
    /// <para>
    /// Cross-resource references are expressed as id-typed string members
    /// (e.g. <c>writer.WriteString("source_id", _sourceId)</c>); there is no
    /// separate links collection on the framework type.
    /// </para>
    /// <para>
    /// Implementations must not write connection strings, credentials, or other
    /// sensitive material into the payload — the resource graph is intended to be
    /// safe to expose to diagnostic UIs.
    /// </para>
    /// </remarks>
    /// <param name="writer">The writer to emit JSON object members into.</param>
    public abstract void Write(Utf8JsonWriter writer);
}
