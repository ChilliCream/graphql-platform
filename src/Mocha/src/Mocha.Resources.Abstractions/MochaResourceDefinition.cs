namespace Mocha.Resources;

/// <summary>
/// Advisory descriptor for a kind of <see cref="MochaResource"/>.
/// </summary>
/// <remarks>
/// Definitions are advisory only — the runtime never validates a resource against its
/// definition. Consumers (typically diagnostic UIs) use the catalog to render kind
/// pickers, group resources by display name, and tooltip the description.
/// </remarks>
/// <param name="Kind">The kind discriminator (e.g. <c>"mocha.queue"</c>) — must match <see cref="MochaResource.Kind"/> exactly.</param>
/// <param name="DisplayName">A short human-readable label suitable for rendering in a UI.</param>
/// <param name="Description">An optional longer description of what this resource kind represents.</param>
public sealed record MochaResourceDefinition(
    string Kind,
    string DisplayName,
    string? Description = null);
