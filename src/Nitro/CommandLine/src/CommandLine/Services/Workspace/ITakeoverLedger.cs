namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Records and queries the immutable audit trail for actor takeovers in an agent workspace.
/// </summary>
internal interface ITakeoverLedger
{
    /// <summary>
    /// Records a takeover and its related items atomically. An empty item collection still records the takeover header.
    /// </summary>
    Task<TakeoverRecord> RecordAsync(
        TakeoverRecordCreation creation,
        IReadOnlyList<TakeoverItem> items,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns takeover records matching every supplied filter, newest first, with their related items.
    /// </summary>
    Task<IReadOnlyList<TakeoverRecord>> QueryAsync(
        TakeoverFilter filter,
        CancellationToken cancellationToken);
}
