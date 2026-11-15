namespace HotChocolate.Fusion.Policies.Auditing;

internal sealed class NoOpAuditScope : IAuditScope
{
    public static NoOpAuditScope Instance { get; } = new();

    private NoOpAuditScope()
    {
    }

    public IAuditTrail Trail => NoOpAuditTrail.Instance;

    public AuditScopeInfo Info => default;

    public AuditSubject Subject => default;

    public bool IsRecording => false;

    public void Record(in AuditLogEntry entry)
    {
    }

    public void Fail(Exception exception)
    {
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
