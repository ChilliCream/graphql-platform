namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// The distinguishing direction of a <see cref="TaskDetailRow"/>: an outgoing
/// dependency of the loaded task, or an incoming dependent that the loaded
/// task blocks.
/// </summary>
internal enum TaskDetailRowKind
{
    Dependency,
    Blocks
}
