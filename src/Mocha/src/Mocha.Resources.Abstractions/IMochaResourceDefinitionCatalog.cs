using System.Diagnostics.CodeAnalysis;

namespace Mocha.Resources;

/// <summary>
/// Read-only catalog of registered <see cref="MochaResourceDefinition"/> entries.
/// </summary>
/// <remarks>
/// Consumed by UIs that want a schema describing all known resource kinds — for
/// rendering kind filters, pretty-printing kind names, and so on.
/// </remarks>
public interface IMochaResourceDefinitionCatalog
{
    /// <summary>
    /// Gets all registered resource definitions.
    /// </summary>
    IReadOnlyList<MochaResourceDefinition> Definitions { get; }

    /// <summary>
    /// Attempts to find the definition registered for the specified <paramref name="kind"/>.
    /// </summary>
    /// <param name="kind">The resource kind discriminator (e.g. <c>"mocha.queue"</c>).</param>
    /// <param name="definition">The matching definition, or <see langword="null"/> if none was registered.</param>
    /// <returns><see langword="true"/> if a definition was found; otherwise <see langword="false"/>.</returns>
    bool TryGet(string kind, [NotNullWhen(true)] out MochaResourceDefinition? definition);
}
