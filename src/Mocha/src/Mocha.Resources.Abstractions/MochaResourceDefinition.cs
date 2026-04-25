namespace Mocha.Resources;

/// <summary>
/// Descriptor for a kind of <see cref="MochaResource"/>.
/// </summary>
/// <param name="Kind">The kind discriminator (e.g. <c>"mocha.queue"</c>) — must match <see cref="MochaResource.Kind"/> exactly.</param>
/// <param name="DisplayName">A short human-readable label suitable for rendering in a UI.</param>
/// <param name="Description">An optional longer description of what this resource kind represents.</param>
public sealed record MochaResourceDefinition(
    string Kind,
    string DisplayName,
    string? Description = null);
