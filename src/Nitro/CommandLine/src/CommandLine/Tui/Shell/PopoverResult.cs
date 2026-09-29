namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// A terminal outcome of <see cref="IPopover.HandleKey"/> that the hosting <see cref="TuiShell"/>
/// is expected to act on. A null return from <see cref="IPopover.HandleKey"/> means the key was
/// consumed without any shell-level effect.
/// </summary>
internal abstract record PopoverResult
{
    private PopoverResult()
    {
    }

    /// <summary>
    /// The popover should be dismissed.
    /// </summary>
    public sealed record Closed : PopoverResult;

    /// <summary>
    /// A tab-specific request, carried opaquely by the shell, that the popover's owning
    /// <see cref="ITuiMode"/> is expected to interpret through its own
    /// <see cref="ITuiMode.HandlePopoverRequest"/>.
    /// </summary>
    public sealed record Request(object Payload) : PopoverResult;
}
