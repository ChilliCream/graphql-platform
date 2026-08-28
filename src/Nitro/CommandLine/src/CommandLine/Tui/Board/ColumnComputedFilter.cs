using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// A computed membership test a column applies on top of its declarative
/// filter, for semantics that <see cref="TaskFilter"/> cannot express alone.
/// </summary>
internal enum ColumnComputedFilter
{
    /// <summary>
    /// No computed test; the declarative filter alone decides membership.
    /// </summary>
    None,

    /// <summary>
    /// Open, unblocked, and not deferred into the future.
    /// </summary>
    Ready,

    /// <summary>
    /// Not closed or tombstoned, and blocked by an unresolved dependency.
    /// </summary>
    Blocked,

    /// <summary>
    /// Deferred: either its status is deferred, or its defer date is in the
    /// future.
    /// </summary>
    Deferred
}
