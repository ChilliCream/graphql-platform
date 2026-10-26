namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// Holds the audit trail that the protected items of one request batch share.
/// The trail is created when the first protected item asks for it.
/// </summary>
internal sealed class AuditInvocation
{
    private object? _sync;
    private IAuditTrail? _trail;

    /// <summary>
    /// Gets the audit trail of the batch and creates it with the provider when no item has done so.
    /// </summary>
    /// <param name="provider">
    /// The provider that creates the trail.
    /// </param>
    /// <param name="requestServices">
    /// The request scoped service provider of the batch.
    /// </param>
    public IAuditTrail GetOrCreateTrail(IAuditProvider provider, IServiceProvider requestServices)
    {
        var trail = Volatile.Read(ref _trail);

        if (trail is not null)
        {
            return trail;
        }

        var created = new object();
        var sync = Interlocked.CompareExchange(ref _sync, created, null) ?? created;

        lock (sync)
        {
            trail = _trail ??= provider.CreateTrail(requestServices);
        }

        return trail;
    }
}
