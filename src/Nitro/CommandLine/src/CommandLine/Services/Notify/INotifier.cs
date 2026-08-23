namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Fires the best-effort wake ping at every live claimed session belonging
/// to the given recipients, after a mail message already committed. Never
/// load-bearing: <see cref="NotifyAsync"/> never throws and never returns a
/// value a caller could branch on, matching the plan's failure policy - a
/// failed ping is a non-event, not something <c>mail send</c>/<c>reply</c>/
/// <c>broadcast</c> retries, reports, or lets affect their own exit code.
/// </summary>
internal interface INotifier
{
    Task NotifyAsync(IReadOnlyList<string> recipientActors, CancellationToken cancellationToken);
}
