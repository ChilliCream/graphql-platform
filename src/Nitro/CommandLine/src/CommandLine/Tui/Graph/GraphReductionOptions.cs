namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Controls the structural reductions applied before graph layout. An absent
/// collapsed-epic set selects the adaptive opening state.
/// </summary>
internal sealed record GraphReductionOptions
{
    public const int AdaptiveCollapseThreshold = 60;

    public const int VisibleNodeCap = 400;

    public bool HideClosed { get; init; } = true;

    public IReadOnlySet<string>? Labels { get; init; }

    public IReadOnlySet<string>? EpicIds { get; init; }

    public IReadOnlySet<string>? CollapsedEpicIds { get; init; }
}
