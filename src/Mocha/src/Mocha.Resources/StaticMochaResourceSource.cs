using Microsoft.Extensions.Primitives;

namespace Mocha.Resources;

/// <summary>
/// A <see cref="MochaResourceSource"/> that exposes a fixed, never-changing list of
/// resources.
/// </summary>
/// <remarks>
/// Mirrors ASP.NET Core's <c>DefaultEndpointDataSource</c>. Suitable for contributors
/// whose resources are determined at startup and do not change for the lifetime of
/// the host. <see cref="GetChangeToken"/> always returns a non-firing token so consumers
/// using <c>ChangeToken.OnChange</c> won't waste registrations.
/// </remarks>
public sealed class StaticMochaResourceSource : MochaResourceSource
{
    private readonly IReadOnlyList<MochaResource> _resources;

    /// <summary>
    /// Initializes a new instance of <see cref="StaticMochaResourceSource"/> with the
    /// supplied resources.
    /// </summary>
    /// <param name="resources">The fixed set of resources this source will expose.</param>
    public StaticMochaResourceSource(IEnumerable<MochaResource> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        _resources = resources.ToArray();
    }

    /// <inheritdoc />
    public override IReadOnlyList<MochaResource> Resources => _resources;

    /// <inheritdoc />
    public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;
}
