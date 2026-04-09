using CookieCrumble;
using CookieCrumble.Formatters;

namespace Mocha.Testing;

/// <summary>
/// CookieCrumble snapshot module that registers Mocha-specific snapshot value formatters.
/// Auto-discovered by CookieCrumble's source generator.
/// </summary>
public sealed class CookieCrumbleMocha : SnapshotModule
{
    /// <inheritdoc />
    protected override IEnumerable<ISnapshotValueFormatter> CreateFormatters()
    {
        yield return TrackedMessagesSnapshotValueFormatter.Instance;
    }
}
